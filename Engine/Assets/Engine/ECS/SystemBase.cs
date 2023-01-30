using System.Collections.Generic;
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

        public static void Awake()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                if (component._active && component._entity.Scene.IsEnabled && component._entity.Transform._activeInHierarchy)
                    component.OnAwake();
        }

        public static void Start()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                if (component._active && component._entity.Scene.IsEnabled && component._entity.Transform._activeInHierarchy)
                    component.OnStart();
        }

        public static void Update()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                if (component._active && component._entity.Scene.IsEnabled && component._entity.Transform._activeInHierarchy)
                    component.OnUpdate();
        }

        public static void LateUpdate()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                if (component._active && component._entity.Scene.IsEnabled && component._entity.Transform._activeInHierarchy)
                    component.OnLateUpdate();
        }

        public static void Render()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                if (component._active && component._entity.Scene.IsEnabled && component._entity.Transform._activeInHierarchy)
                    component.OnRender();
        }

        public static void Destroy(T component)
        {
            s_components.Remove(component);
            component.OnDestroy();
        }
    }

    class CameraSystem : SystemBase<Camera> { }
    class TransformSystem : SystemBase<Transform> { }
    class MeshSystem : SystemBase<Mesh> { }
    class ScriptSystem : SystemBase<Component> { }
    class EditorScriptSystem : SystemBase<EditorComponent> { }
}
