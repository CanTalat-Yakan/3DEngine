using System.Collections.Generic;
using System.Numerics;
using System;
using Engine.Components;
using Engine.ECS;
using Engine.Helper;
using Editor.Controller;
using System.IO;

namespace Engine.Utilities
{
    internal enum EPrimitiveTypes
    {
        Plane,
        Quad,
        Cube,
        Sphere,
        Icosphere,
        Cylinder,
        //Capsule,
        //Cone,
        Torus,
        Tree,
        Suzanne,
        Duck,
    }

    internal class EventList<T> : List<T>
    {
        public event EventHandler<T> OnAddEvent;
        public event EventHandler<T> OnRemoveEvent;

        public void Add(T item, bool invokeEvent = true)
        {
            base.Add(item);

            if (null != OnAddEvent)
                if (invokeEvent)
                    OnAddEvent(this, item);
        }

        public void Remove(T item, bool invokeEvent = true)
        {
            if (null != OnRemoveEvent)
                if (invokeEvent)
                    OnRemoveEvent(this, item);

            base.Remove(item);
        }
    }

    internal class EntityManager
    {
        public EventList<Entity> EntityList = new EventList<Entity>();
        public Entity Sky;

        private Material _materialDefault;
        private Material _materialReflection;
        private Material _materialSky;
        private Material _materialSkyLight;

        private static readonly string SHADER_LIT = @"Shader\Lit.hlsl";
        private static readonly string SHADER_SIMPLELIT = @"Shader\SimpleLit.hlsl";
        private static readonly string SHADER_UNLIT = @"Shader\Unlit.hlsl";

        private static readonly string IMAGE_DEFAULT = @"Textures\dark.png";
        private static readonly string IMAGE_SKY = @"Textures\SkyGradient.png";
        private static readonly string IMAGE_SKY_LIGHT = @"Textures\SkyGradient_Light.png";

        private static readonly string PATH_PRIMITIVES = @"Models\Primitives";

        public EntityManager()
        {
            _materialDefault = new(SHADER_SIMPLELIT, IMAGE_DEFAULT);
            _materialReflection = new(SHADER_LIT, IMAGE_SKY);
            _materialSky = new(SHADER_UNLIT, IMAGE_SKY);
            _materialSkyLight = new(SHADER_UNLIT, IMAGE_SKY_LIGHT);
        }

        public T[] FindComponent<T>() where T : Component
        {
            List<T> components = new();
            foreach (var entity in EntityList)
                foreach (var component in entity.GetComponents<T>())
                    if (component.GetType().Equals(typeof(T)))
                        components.Add(component);

            return components.ToArray();
        }

        public Entity Duplicate(Entity refEntity, Entity parent = null)
        {
            Entity clonedEntity = refEntity.Clone();
            clonedEntity.Parent = parent;

            EntityList.Add(clonedEntity);

            return clonedEntity;
        }

        public Entity CreateEntity(Entity parent = null, string name = "New Entity")
        {
            Entity newEntity = new()
            {
                Name = name,
                Parent = parent
            };

            EntityList.Add(newEntity);

            return newEntity;
        }

        public Entity CreatePrimitive(EPrimitiveTypes type, Entity parent = null)
        {
            Entity newEntity = new();
            newEntity.Name = type.ToString().FormatString();
            newEntity.Parent = parent;
            
            newEntity.AddComponent(new Mesh(ModelLoader.LoadFilePro(Path.Combine(PATH_PRIMITIVES, type.ToString()) + ".obj")));
            newEntity.GetComponent<Mesh>().Material = _materialDefault;

            EntityList.Add(newEntity);

            return newEntity;
        }

        public void CreateSky()
        {
            Sky = new() { Name = "Sky" };
            Sky.Transform.Scale = new Vector3(-1000, 1000, 1000);

            Sky.AddComponent(new Mesh(ModelLoader.LoadFilePro(Path.Combine(PATH_PRIMITIVES, EPrimitiveTypes.Sphere.ToString()) + ".obj")));
            Sky.GetComponent<Mesh>().Material = _materialSky;

            EntityList.Add(Sky);
        }

        public Entity GetFromID(Guid id)
        {
            foreach (var entity in EntityList)
                if (entity != null)
                    if (entity.ID == id)
                        return entity;

            return null;
        }

        public void SetTheme(bool light) => Sky.GetComponent<Mesh>().Material = light ? _materialSkyLight : _materialSky;

        public void Destroy(Entity sourceEntity) => EntityList.Remove(sourceEntity);
    }
}
