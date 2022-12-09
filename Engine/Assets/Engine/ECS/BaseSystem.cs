using System.Collections.Generic;
using Engine.Components;

namespace Engine.ECS
{
    internal class BaseSystem<T> where T : Component
    {
        protected static List<T> _components = new();

        public static void Register(T component)
        {
            _components.Add(component);
        }

        public static void Awake()
        {
            foreach (T component in _components)
                component.Awake();
        }
        public static void Start()
        {
            foreach (T component in _components)
                component.Start();
        }
        public static void Update()
        {
            foreach (T component in _components)
                component.Update();
        }
        public static void LateUpdate()
        {
            foreach (T component in _components)
                component.LateUpdate();
        }
        public static void Render()
        {
            foreach (T component in _components)
                component.Render();
        }
    }

    class CameraSystem : BaseSystem<Camera> { }
    class TransformSystem : BaseSystem<Transform> { }
    class MeshSystem : BaseSystem<Mesh> { }
    class ScriptSystem : BaseSystem<Component> { }
}
