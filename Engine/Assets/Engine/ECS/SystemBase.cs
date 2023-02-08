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
            // Adds the given component to the static list of components.
            s_components.Add(component);

            // Register the component's OnDestroy event.
            component._eventOnDestroy += (s, e) => Destroy(component);
        }

        public static void Sort() =>
            // Sort components based on the Order value.
            s_components = s_components.OrderBy(Component => Component.Order).ToList();

        public static void Awake()
        {
            // Loop through all the components in the static components array
            // and call OnAwake method on the component if it is active.
            foreach (T component in s_components.ToArray())
                if (CheckActive(component))
                    component.OnAwake();
        }

        public static void Start()
        {
            // Loop through all the components in the static components array
            // and call OnStart method on the component if it is active.
            foreach (T component in s_components.ToArray())
                if (CheckActive(component))
                    component.OnStart();
        }

        public static void Update()
        {
            // Loop through all the components in the static components array
            // and call OnUpdate method on the component if it is active.
            foreach (T component in s_components.ToArray())
                if (CheckActive(component))
                    component.OnUpdate();
        }

        public static void LateUpdate()
        {
            // Loop through all the components in the static components array
            // and call OnLateUpdate method on the component if it is active.
            foreach (T component in s_components.ToArray())
                if (CheckActive(component))
                    component.OnLateUpdate();
        }

        public static void Render()
        {
            // Loop through all the components in the static components array
            // and call OnRender method on the component if it is active.
            foreach (T component in s_components.ToArray())
                if (CheckActive(component))
                    component.OnRender();
        }

        public static void Destroy(T component)
        {
            // Remove the specified component from the collection of registered components
            // and trigger the OnDestroy event for the component.
            s_components.Remove(component);
            component.OnDestroy();
        }

        private static bool CheckActive(T component)
        {
            // Check if the component is active.
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
