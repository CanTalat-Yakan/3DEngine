using System.Collections.Generic;
using System.Linq;
using Engine.Components;

namespace Engine.ECS
{
    internal class SystemBase<T> where T : Component
    {
        protected static List<T> s_components = new();

        public static void Register(T component)
        {
            s_components.Add(component);
            component._eventOnDestroy += (s, e) => Destroy(component);
        }

        public static void Sort() =>
            s_components = s_components.OrderBy(Component => Component.Order).ToList();

        public static void Awake()
        {
            foreach (T component in s_components.ToArray())
                if (CheckActive(component))
                    component.OnAwake();
        }

        public static void Start()
        {
            foreach (T component in s_components.ToArray())
                if (CheckActive(component))
                    component.OnStart();
        }

        public static void Update()
        {
            foreach (T component in s_components.ToArray())
                if (CheckActive(component))
                    component.OnUpdate();
        }

        public static void LateUpdate()
        {
            foreach (T component in s_components.ToArray())
                if (CheckActive(component))
                    component.OnLateUpdate();
        }

        public static void Render()
        {
            foreach (T component in s_components.ToArray())
                if (CheckActive(component))
                    component.OnRender();
        }

        public static void Destroy(T component)
        {
            s_components.Remove(component);
            component.OnDestroy();
        }

        private static bool CheckActive(T component)
        {
            return
                component.IsEnabled &&
                component.Entity.IsEnabled &&
                component.Entity.Scene.IsEnabled &&
                component.Entity.ActiveInHierarchy; 
        }
    }

    class CameraSystem : SystemBase<Camera> { }
    class TransformSystem : SystemBase<Transform> { }
    class MeshSystem : SystemBase<Mesh> { }
    class ScriptSystem : SystemBase<Component> { }
    class EditorScriptSystem : SystemBase<EditorComponent> { }
}
