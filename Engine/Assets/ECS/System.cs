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
    public static T[] Components => s_components.ToArray();
    private static IEnumerable<T> s_components;

    private static bool s_dirty = true;

    public static void Register(T component)
    {
        Kernel.Instance.SystemManager.ComponentManager.AddComponent(component.Entity, component);

        // Register the OnDestroy event of the component.
        component.EventOnDestroy += () => Destroy(component);

        s_dirty = true;
    }

    public static void FetchArray(bool sort = false)
    {
        if (!s_dirty)
            return;

        s_components = Kernel.Instance.SystemManager.ComponentManager.GetDenseArray<T>();

        if (sort)
            s_components = s_components.OrderBy(Component => Component.Order);

        s_dirty = false;
    }

    public static void Destroy(T component)
    {
        component.OnDestroy();

        s_dirty = true;
    }

    public static void Destroy(Type componentType)
    {
        foreach (var component in s_components
            .Where(c => c.GetType() == componentType)
            .ToArray())
        {
            Destroy(component);

            component.Entity.RemoveComponent(component);
        }
    }

    public static void Destroy()
    {
        foreach (var component in s_components)
            component.OnDestroy();

        s_components = null;
    }

    public static void Replace(Type oldComponentType, Type newComponentType)
    {
        foreach (var component in s_components
            .Where(Component => Component.GetType() == oldComponentType)
            .ToArray())
        {
            Destroy(component);

            component.Entity.RemoveComponent(component);
            component.Entity.AddComponent(newComponentType);
        }
    }

    private static bool CheckActive(T component) =>
           component.IsEnabled
        && component.Entity.Data.IsEnabled
        && component.Entity.Manager.IsEnabled
        && component.Entity.Data.ActiveInHierarchy;
}

public partial class System<T> where T : Component
{
    private static ParallelOptions _parallelOptions = new() { MaxDegreeOfParallelism = 20 };

    public static void Awake()
    {
        // Loop through all the components in the static components array
        // and call OnAwake method on the component if it is active.
        Parallel.ForEach(s_components, _parallelOptions, component =>
        {
            if (CheckActive(component))
                component.OnAwake();
        });
    }

    public static void Start()
    {
        Parallel.ForEach(s_components, _parallelOptions, component =>
        {
            if (CheckActive(component))
                component.OnStart();
        });
    }

    public static void Update()
    {
        Parallel.ForEach(s_components, _parallelOptions, component =>
        {
            if (CheckActive(component))
                component.OnUpdate();
        });
    }

    public static void LateUpdate()
    {
        Parallel.ForEach(s_components, _parallelOptions, component =>
        {
            if (CheckActive(component))
                component.OnLateUpdate();
        });
    }

    public static void FixedUpdate()
    {
        Parallel.ForEach(s_components, _parallelOptions, component =>
        {
            if (CheckActive(component))
                component.OnFixedUpdate();
        });
    }

    public static void Render()
    {
        Parallel.ForEach(s_components, _parallelOptions, component =>
        {
            if (CheckActive(component))
                component.OnRender();
        });
    }

    public static void FixedRender()
    {
        // Loop through all the components in the static components array
        // and call OnRender method on the component if it is active.
        foreach (T component in s_components) // This will run in a separate thread,
                                              // asynchronously reprojecting the render target texture.
            if (CheckActive(component))
                component.OnFixedRender();
    }

    public static void GUI()
    {
        foreach (T component in s_components)
            if (CheckActive(component))
                component.OnGUI();
    }
}