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
                if (component._active)
                    component.Awake();
        }

        public static void Start()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                if (component._active)
                    component.Start();
        }

        public static void Update()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                if (component._active)
                    component.Update();
        }

        public static void LateUpdate()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                if (component._active)
                    component.LateUpdate();
        }

        public static void Render()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                if (component._active)
                    component.Render();
        }

        public static void Destroy(T component)
        {
            s_components.Remove(component);
            component.Destroy();
        }
    }

    class CameraSystem : SystemBase<Camera> { }
    class TransformSystem : SystemBase<Transform> { }
    class MeshSystem : SystemBase<Mesh> { }
    class ScriptSystem : SystemBase<Component> { }
    class EditorScriptSystem : SystemBase<EditorComponent> { }
}
