using System.Collections.Generic;
using Engine.Components;

namespace Engine.ECS
{
    internal class BaseSystem<T> where T : Component
    {
        protected static List<T> s_components = new();

        public static void Register(T component) => s_components.Add(component);

        public static void Awake()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                component.Awake();
        }

        public static void Start()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                component.Start();
        }

        public static void Update()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                component.Update();
        }

        public static void LateUpdate()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                component.LateUpdate();
        }

        public static void Render()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                component.Render();
        }
    }

    class CameraSystem : BaseSystem<Camera> { }
    class TransformSystem : BaseSystem<Transform> { }
    class MeshSystem : BaseSystem<Mesh> { }
    class ScriptSystem : BaseSystem<Component> { }
}
