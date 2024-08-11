using System.Collections.Generic;

namespace Engine.ECS;

public sealed partial class ComponentManager
{
    private Dictionary<Type, SparseSet> _componentSparseSets = new();

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

    public T GetComponent<T>(Entity entity) where T : Component
    {
        if (_componentSparseSets.TryGetValue(typeof(T), out var sparseSet))
        {
            var typedSparseSet = (SparseSet<T>)sparseSet;
            return typedSparseSet.Get(entity);
        }

        return null;
    }

    public Component GetComponent(Entity entity, Type componentType)
    {
        if (_componentSparseSets.TryGetValue(componentType, out var sparseSet))
        {
            var getMethod = sparseSet.GetType().GetMethod("Get");
            return (Component)getMethod.Invoke(sparseSet, [entity]);
        }

        return null;
    }
    public Type[] GetComponents(Entity entity)
    {
        var componentTypes = new List<Type>();

        foreach (var sparseSet in _componentSparseSets.Values)
        {
            var getMethod = sparseSet.GetType().GetMethod("Get");
            var component = getMethod.Invoke(sparseSet, [entity]);

            if (component is not null)
                componentTypes.Add(component.GetType());
        }

        return componentTypes.ToArray();
    }
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
            public Dictionary<int, int> SparseArray { get; } = new(); // Maps entity ID to dense index
            public List<T> DenseArray { get; } = new(); // List of components
            public List<int> EntityIDArray { get; } = new(); // List of entity IDs
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

        private int GetPageNumber(int entityId) => entityId / MaxPageSize;

        public void Add(Entity entity, T component)
        {
            int pageNumber = GetPageNumber(entity.ID);
            var page = GetPage(pageNumber);

            if (!page.SparseArray.ContainsKey(entity.ID))
            {
                page.SparseArray[entity.ID] = page.DenseArray.Count;
                page.DenseArray.Add(component);
                page.EntityIDArray.Add(entity.ID);
            }
        }

        public T Get(Entity entity)
        {
            int pageNumber = GetPageNumber(entity.ID);
            if (_pages.TryGetValue(pageNumber, out var page) &&
                page.SparseArray.TryGetValue(entity.ID, out int denseIndex))
            {
                return page.DenseArray[denseIndex];
            }

            return default;
        }

        public void Remove(Entity entity)
        {
            int pageNumber = GetPageNumber(entity.ID);
            if (_pages.TryGetValue(pageNumber, out var page) &&
                page.SparseArray.TryGetValue(entity.ID, out int denseIndex))
            {
                // Invoke the destruction event before removing the component
                page.DenseArray[denseIndex].InvokeEventOnDestroy();

                int lastDenseIndex = page.DenseArray.Count - 1;

                // Swap the last element with the element to be removed
                page.DenseArray[denseIndex] = page.DenseArray[lastDenseIndex];
                page.EntityIDArray[denseIndex] = page.EntityIDArray[lastDenseIndex];

                // Update the sparse array to point to the new location
                page.SparseArray[page.EntityIDArray[denseIndex]] = denseIndex;

                // Remove the last element
                page.DenseArray.RemoveAt(lastDenseIndex);
                page.EntityIDArray.RemoveAt(lastDenseIndex);

                page.SparseArray.Remove(entity.ID);

                // Optionally remove the page if it's empty
                if (page.DenseArray.Count == 0)
                {
                    _pages.Remove(pageNumber);
                }
            }
        }

        public IEnumerable<T> GetAllComponents()
        {
            foreach (var page in _pages.Values)
                foreach (var component in page.DenseArray)
                    yield return component;
        }

        public IEnumerable<int> GetAllEntityIDs()
        {
            foreach (var page in _pages.Values)
                foreach (var id in page.EntityIDArray)
                    yield return id;
        }
    }
}
