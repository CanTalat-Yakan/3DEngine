using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Engine.ECS;

internal sealed class TransformSystem : System<Transform> { }
internal sealed class CameraSystem : System<Camera> { }
internal sealed class MeshSystem : System<Mesh> { }

public sealed class ScriptSystem : System<Component> { }
public sealed class EditorScriptSystem : System<EditorComponent> { }

public partial class System<T> where T : Component
{
    public static T[] Components => s_componentsArray;

    private static List<T> s_components = new();
    private static T[] s_componentsArray;

    private static bool s_dirty = true;

    public static void Register(T component)
    {
        // Adds the given component to the static list of components.
        s_components.Add(component);

        // Register the OnDestroy event of the component.
        component.EventOnDestroy += () => Destroy(component);

        s_dirty = true;
    }

    public static void Destroy(T component)
    {
        // Remove the specified component from the collection of registered components.
        s_components.Remove(component);

        // Trigger the OnDestroy event for the component.
        component.OnDestroy();

        s_dirty = true;
    }

    public static void Destroy(Type componentType)
    {
        // Remove all components of the specified type from the collection of registered components.
        foreach (var component in s_components
            .Where(c => c.GetType() == componentType)
            .ToArray())
        {
            Destroy(component);

            component.Entity.RemoveComponent(component);
        }
    }

    public static void Dispose()
    {
        // Remove all components and call OnDestroy().
        foreach (var component in s_components)
            component.OnDestroy();

        s_components.Clear();
        s_componentsArray = null;
    }

    public static void Replace(Type oldComponentType, Type newComponentType)
    {
        // Remove all components of the specified type from the collection of registered components.
        foreach (var component in s_components
            .Where(Component => Component.GetType() == oldComponentType)
            .ToArray())
        {
            Destroy(component);

            component.Entity.RemoveComponent(component);
            component.Entity.AddComponent(newComponentType);
        }
    }

    internal static void CopyToArray() =>
        s_componentsArray = s_components.ToArray();

    internal static void SortAndCopyToArray() =>
        s_componentsArray = s_components
        .OrderBy(Component => Component.Order)
        .ToArray();

    internal static void SortAndCopyToArrayIfDirty()
    {
        if (s_dirty)
        {
            SortAndCopyToArray();

            s_dirty = false;
        }
    }

    private static bool CheckActive(T component) =>
        // Check if the component is active.
           component.IsEnabled
        && component.Entity.IsEnabled
        && component.Entity.Scene.IsEnabled
        && component.Entity.ActiveInHierarchy;
}

public partial class System<T> where T : Component
{
    private static ParallelOptions _parallelOptions = new() { MaxDegreeOfParallelism = 1 };

    public static void Awake()
    {
        // Loop through all the components in the static components array
        // and call OnAwake method on the component if it is active.
        Parallel.ForEach(s_componentsArray, _parallelOptions, component =>
        {
            if (CheckActive(component))
                component.OnAwake();
        });
    }

    public static void Start()
    {
        // Loop through all the components in the static components array
        // and call OnStart method on the component if it is active.
        Parallel.ForEach(s_componentsArray, _parallelOptions, component =>
        {
            if (CheckActive(component))
                component.OnStart();
        });
    }

    public static void Update()
    {
        // Loop through all the components in the static components array
        // and call OnUpdate method on the component if it is active.
        Parallel.ForEach(s_componentsArray, _parallelOptions, component =>
        {
            if (CheckActive(component))
                component.OnUpdate();
        });
    }

    public static void LateUpdate()
    {
        // Loop through all the components in the static components array
        // and call OnLateUpdate method on the component if it is active.
        Parallel.ForEach(s_componentsArray, _parallelOptions, component =>
        {
            if (CheckActive(component))
                component.OnLateUpdate();
        });
    }

    public static void FixedUpdate()
    {
        // Loop through all the components in the static components array
        // and call OnLateUpdate method on the component if it is active.
        Parallel.ForEach(s_componentsArray, _parallelOptions, component =>
        {
            if (CheckActive(component))
                component.OnFixedUpdate();
        });
    }

    public static void Render()
    {
        // Loop through all the components in the static components array
        // and call OnRender method on the component if it is active.
        foreach (T component in s_componentsArray) // This will run in a separate thread, asynchronously reprojecting the render target texture
            if (CheckActive(component))
                component.OnRender();
    }

    public static void GUI()
    {
        // Loop through all the components in the static components array
        // and call OnGUI method on the component if it is active.
        foreach (T component in s_componentsArray)
            if (CheckActive(component))
                component.OnGUI();
    }
}