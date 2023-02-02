﻿using System.Collections.Generic;
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
            var components = s_components.ToArray();
            foreach (T component in components)
                if (CheckActive(component))
                    component.OnAwake();
        }

        public static void Start()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                if (CheckActive(component))
                    component.OnStart();
        }

        public static void Update()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                if (CheckActive(component))
                    component.OnUpdate();
        }

        public static void LateUpdate()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
                if (CheckActive(component))
                    component.OnLateUpdate();
        }

        public static void Render()
        {
            var components = s_components.ToArray();
            foreach (T component in components)
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
            return component.IsEnabled && component.Entity.IsEnabled && component.Entity.ActiveInHierarchy && component.Entity.Scene.IsEnabled;
        }
    }

    class CameraSystem : SystemBase<Camera> { }
    class TransformSystem : SystemBase<Transform> { }
    class MeshSystem : SystemBase<Mesh> { }
    class ScriptSystem : SystemBase<Component> { }
    class EditorScriptSystem : SystemBase<EditorComponent> { }
}
