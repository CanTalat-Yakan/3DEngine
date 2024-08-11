using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Engine.ECS;

internal sealed class TransformSystem : System<Transform> { }
internal sealed class CameraSystem : System<Camera> { }
internal sealed class MeshSystem : System<Mesh> { }

public sealed class ScriptSystem : System<Component> { }
public sealed class EditorScriptSystem : System<EditorComponent> { }
public sealed class SimpleSystem : System<SimpleComponent> { }

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

    private static void IfActive(T component, Action action)
    {
        if (CheckActive(component))
            action.Invoke();
    }
}

public partial class System<T> where T : Component
{
    private static ParallelOptions _options = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };

    public static void Awake() =>
        Parallel.ForEach(s_components, _options, component => IfActive(component, component.OnAwake));

    public static void Start() =>
        Parallel.ForEach(s_components, _options, component => IfActive(component, component.OnStart));

    public static void Update() =>
        Parallel.ForEach(s_components, _options, component => IfActive(component, component.OnUpdate));

    public static void SimpleUpdate() =>
        Parallel.ForEach(s_components, _options, component => component.OnUpdate());

    public static void LateUpdate() =>
        Parallel.ForEach(s_components, _options, component => IfActive(component, component.OnLateUpdate));

    public static void FixedUpdate() =>
        Parallel.ForEach(s_components, _options, component => IfActive(component, component.OnFixedUpdate));

    public static void Render()
    {
        // This will run in a separate thread, asynchronously reprojecting the render target texture.
        foreach (T component in s_components)
            IfActive(component, component.OnRender);
    }

    public static void GUI() =>
        Parallel.ForEach(s_components, _options, component => IfActive(component, component.OnGUI));
}