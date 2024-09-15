using System.Collections.Generic;
using System.Linq;
using Vortice.WIC;

namespace Engine.ECS;

public sealed partial class ComponentManager
{
    private Dictionary<Type, SparseSet> _componentSparseSets = new();

    private Dictionary<Type, Type> _typeMap = new()
    {
        { typeof(Transform), typeof(Transform) },
        { typeof(Camera), typeof(Camera) },
        { typeof(Mesh), typeof(Mesh) },

        { typeof(Component), typeof(Component) },
        { typeof(EditorComponent), typeof(EditorComponent) },
        { typeof(SimpleComponent), typeof(SimpleComponent) },
    };

    public Type GetBaseComponentType(Type componentType)
    {
        if (_typeMap.TryGetValue(componentType, out var baseComponentType)
         || _typeMap.TryGetValue(componentType.BaseType, out baseComponentType))
            return baseComponentType;

        return componentType;
    }

    public IEnumerable<T> GetDenseArray<T>() where T : Component
    {
        if (_componentSparseSets.TryGetValue(typeof(T), out var sparseSet))
            return ((SparseSet<T>)sparseSet).GetAllComponents();
        else
            return Array.Empty<T>();
    }

    public void AddComponent<T>(Entity entity, T component) where T : Component
    {
        if (!_componentSparseSets.TryGetValue(typeof(T), out var sparseSet))
        {
            sparseSet = new SparseSet<T>();
            _componentSparseSets[typeof(T)] = sparseSet;
        }

        var typedSparseSet = (SparseSet<T>)sparseSet;
        typedSparseSet.Add(entity, component);
    }

    public T[] GetComponent<T>(Entity entity) where T : Component
    {
        if (_componentSparseSets.TryGetValue(GetBaseComponentType(typeof(T)), out var sparseSet))
        {
            var typedSparseSet = (SparseSet<T>)sparseSet;
            var components = typedSparseSet.Get(entity);

            return components;
        }

        return null;
    }

    public Component[] GetComponent(Entity entity, Type componentType)
    {
        if (_componentSparseSets.TryGetValue(GetBaseComponentType(componentType), out var sparseSet))
        {
            var getMethod = sparseSet.GetType().GetMethod("Get");
            var components = (Component[])getMethod.Invoke(sparseSet, [entity]);

            return components;
        }

        return Array.Empty<Component>();
    }

    public Component[] GetComponents(Entity entity)
    {
        List<Component> componentCollection = new();

        foreach (var sparseSet in _componentSparseSets.Values)
        {
            var getMethod = sparseSet.GetType().GetMethod("Get");
            var components = getMethod.Invoke(sparseSet, [entity]);

            if (components is not null)
                componentCollection.AddRange((Component[])components);
        }

        return componentCollection.ToArray();
    }

    public Type[] GetComponentTypes(Entity entity) =>
        GetComponents(entity).Select(component => component.GetType()).ToArray();

    public void RemoveComponent<T>(Entity entity) where T : Component
    {
        if (_componentSparseSets.TryGetValue(typeof(T), out var sparseSet))
        {
            var typedSparseSet = (SparseSet<T>)sparseSet;
            typedSparseSet.Remove(entity);
        }
    }

    public void RemoveComponent(Entity entity, Type componentType)
    {
        if (_componentSparseSets.TryGetValue(componentType, out var sparseSet))
        {
            var removeMethod = sparseSet.GetType().GetMethod("Remove");
            removeMethod.Invoke(sparseSet, [entity]);
        }
    }

    public void RemoveComponents(Entity entity)
    {
        foreach (var sparseSet in _componentSparseSets.Values)
        {
            var removeMethod = sparseSet.GetType().GetMethod("Remove");
            removeMethod.Invoke(sparseSet, [entity]);
        }
    }
}

public sealed partial class ComponentManager
{
    private abstract class SparseSet { }

    private class SparseSet<T> : SparseSet where T : Component
    {
        private const int MaxPageSize = 1_000; // Define the maximum size of a page
        private Dictionary<int, SparsePage> _pages = new(); // Maps page number to a SparsePage

        private class SparsePage
        {
            public Dictionary<int, List<int>> SparseArray { get; } = new(); // Maps entity ID to a list of dense indices
            public List<T> DenseArray { get; } = new(); // List of components
        }

        private SparsePage GetPage(int pageNumber)
        {
            if (!_pages.TryGetValue(pageNumber, out var page))
            {
                page = new SparsePage();
                _pages[pageNumber] = page;
            }
            return page;
        }

        private int GetPageNumber(int entityId) =>
            entityId / MaxPageSize;

        public T[] Get(Entity entity)
        {
            // Calculate the page number for the entity's ID
            int pageNumber = GetPageNumber(entity.ID);

            // Check if the page and the sparse array entry for the entity exist
            if (_pages.TryGetValue(pageNumber, out var page) &&
                page.SparseArray.TryGetValue(entity.ID, out List<int> denseIndices))
            {
                // Create an array to store the components of type T
                var componentArray = new T[denseIndices.Count];

                // Populate the array with the components from the dense array
                for (int i = 0; i < denseIndices.Count; i++)
                    componentArray[i] = page.DenseArray[denseIndices[i]];

                return componentArray;
            }

            return null;
        }

        public void Add(Entity entity, T component)
        {
            int pageNumber = GetPageNumber(entity.ID);
            var page = GetPage(pageNumber);

            if (!page.SparseArray.ContainsKey(entity.ID))
            {
                page.SparseArray[entity.ID] = new List<int>();
            }

            // Add the component to the dense array and keep track of the index
            int index = page.DenseArray.Count;
            page.DenseArray.Add(component);
            page.SparseArray[entity.ID].Add(index);
        }

        public void Remove(Entity entity)
        {
            int pageNumber = GetPageNumber(entity.ID);
            if (_pages.TryGetValue(pageNumber, out var page) && page.SparseArray.TryGetValue(entity.ID, out List<int> denseIndices))
            {
                // Sort dense indices in descending order to avoid shifting problems during removal
                denseIndices.Sort((a, b) => b.CompareTo(a));

                foreach (var denseIndex in denseIndices)
                {
                    // Invoke the destruction event for the component
                    page.DenseArray[denseIndex].Dispose();

                    int lastDenseIndex = page.DenseArray.Count - 1;

                    if (denseIndex != lastDenseIndex)
                    {
                        // Swap the last element with the element to be removed
                        page.DenseArray[denseIndex] = page.DenseArray[lastDenseIndex];

                        // Update the sparse array for the entity that was at the last position
                        foreach (var key in page.SparseArray.Keys)
                        {
                            var indices = page.SparseArray[key];
                            for (int i = 0; i < indices.Count; i++)
                                if (indices[i] == lastDenseIndex)
                                {
                                    indices[i] = denseIndex;
                                    break;
                                }
                        }
                    }

                    // Remove the last element from the dense array
                    page.DenseArray.RemoveAt(lastDenseIndex);
                }

                // Remove the entity entry if all components are removed
                page.SparseArray.Remove(entity.ID);

                // Optionally remove the page if it's empty
                if (page.DenseArray.Count == 0)
                    _pages.Remove(pageNumber);
            }
        }

        public IEnumerable<T> GetAllComponents()
        {
            foreach (var page in _pages.Values)
            {
                var denseArray = page.DenseArray;
                for (int i = 0; i < denseArray.Count; i++)
                    yield return denseArray[i];
            }
        }
    }
}