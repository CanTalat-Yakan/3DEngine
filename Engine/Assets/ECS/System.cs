using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using EnTTSharp.Entities;
using EnTTSharp.Entities.Pools;
using Microsoft.Win32;

namespace Engine.ECS;

internal sealed class EntityDataSystem : SystemBase<EntityData> { }
internal sealed class TransformSystem : SystemBase<Transform> { }
internal sealed class CameraSystem : SystemBase<Camera> { }
internal sealed class MeshSystem : SystemBase<Mesh> { }

public sealed class ScriptSystem : SystemBase<Component> { }
public sealed class EditorScriptSystem : SystemBase<EditorComponent> { }

public partial class SystemBase<T> where T : Component
{
    protected readonly static List<EntityRegistry<EntityKey>> Registries = new();

    public static IReadOnlyPool<EntityKey, T> ComponentPool => _componentPool ??= GetCombinedPool();
    private static IReadOnlyPool<EntityKey, T> _componentPool;

    private static bool s_isDirty;

    public static IReadOnlyPool<EntityKey, T> GetCombinedPool()
    {
        List<IReadOnlyPool<EntityKey, T>> pools = new();

        foreach (var registry in Registries)
            pools.Add(registry.GetPool<T>());

        Pool<EntityKey, T> combinedPool = new();

        foreach (var pool in pools)
            foreach (var entityComponent in pool)
                combinedPool.Add(entityComponent);

        return combinedPool;
    }

    public static void Register(T component)
    {
        var registry = component.EntityData.EntityManager.Registry;

        RegisterComponent(registry, component);

        component.EventOnDestroy += () => Destroy(component);

        Registries.Add(registry);

        s_isDirty = true;
    }

    public static void RegisterComponent(EntityRegistry<EntityKey> registry, T component)
    {
        bool componentRegistered = false;
        foreach (var componentType in ComponentPool.Select(component => component.GetType()))
            if (componentType.Equals(typeof(T)))
                componentRegistered = true;

        if (!componentRegistered)
        {
            // Use reflection to get the first method named 'Register'
            var registerMethod = registry.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(method => method.Name == "Register");
            var genericMethod = registerMethod.MakeGenericMethod(component.GetType());

            genericMethod.Invoke(registry, null);
        }
    }
}

public partial class SystemBase<T> where T : Component
{
    public static void Destroy(T component)
    {
        component.OnDestroy();

        component.EntityData.EntityManager.Registry.RemoveComponent<T>(component.EntityKey);

        s_isDirty = true;
    }

    public static void Destroy(Type componentType) { }

    public static void Destroy() { }

    public static void Replace(Type oldComponentType, Type newComponentType) { }

    public static void CachePoolIfDirty()
    {
        if (!s_isDirty)
            return;

        _componentPool = null;
        _ = ComponentPool;
    }

    private static bool CheckActive(T component) =>
            component.IsEnabled
         && component.EntityData.IsEnabled
         && component.EntityData.EntityManager.IsEnabled
         && component.EntityData.IsActiveInHierarchy;
}

public partial class SystemBase<T> where T : Component
{
    private static ParallelOptions _parallelOptions = new() { MaxDegreeOfParallelism = 20 };

    public static void Awake() =>
        Parallel.ForEach(ComponentPool, _parallelOptions, entity =>
        {
            ComponentPool.TryGet(entity, out var component);
            if (CheckActive(component))
                component.OnAwake();
        });

    public static void Start() =>
        Parallel.ForEach(ComponentPool, _parallelOptions, entity =>
        {
            ComponentPool.TryGet(entity, out var component);
            if (CheckActive(component))
                component.OnStart();
        });

    public static void Update() =>
        Parallel.ForEach(ComponentPool, _parallelOptions, entity =>
        {
            ComponentPool.TryGet(entity, out var component);
            if (CheckActive(component))
                component.OnUpdate();
        });

    public static void LateUpdate() =>
        Parallel.ForEach(ComponentPool, _parallelOptions, entity =>
        {
            ComponentPool.TryGet(entity, out var component);
            if (CheckActive(component))
                component.OnLateUpdate();
        });

    public static void FixedUpdate() =>
        Parallel.ForEach(ComponentPool, _parallelOptions, entity =>
        {
            ComponentPool.TryGet(entity, out var component);
            if (CheckActive(component))
                component.OnFixedUpdate();
        });

    public static void Render()
    {
        // Loop through all the components in the static components array
        // and call OnRender method on the component if it is active.
        foreach (EntityKey entity in ComponentPool) // This will run in a separate thread,
        {                                           // asynchronously reprojecting the render target texture.
            ComponentPool.TryGet(entity, out var component);
            if (CheckActive(component))
                component.OnRender();
        }
    }

    public static void GUI()
    {
        foreach (EntityKey entity in ComponentPool)
        {
            ComponentPool.TryGet(entity, out var component);
            if (CheckActive(component))
                component.OnGUI();
        }
    }
}