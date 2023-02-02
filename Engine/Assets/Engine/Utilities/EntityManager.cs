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
        private Material _materialSky;
        private Material _materialSkyLight;

        private static readonly string SHADER_LIT = @"Shader\Lit.hlsl";
        private static readonly string SHADER_SIMPLELIT = @"Shader\SimpleLit.hlsl";
        private static readonly string SHADER_UNLIT = @"Shader\Unlit.hlsl";
        private static readonly string SHADER_SKY = @"Shader\Sky.hlsl";

        private static readonly string IMAGE_DEFAULT = @"Textures\dark.png";
        private static readonly string IMAGE_SKY = @"Textures\SkyGradient.png";
        private static readonly string IMAGE_SKY_LIGHT = @"Textures\SkyGradient_Light.png";

        private static readonly string PATH_PRIMITIVES = @"Models\Primitives";

        public EntityManager()
        {
            _materialDefault = new(SHADER_SIMPLELIT, IMAGE_DEFAULT);
            _materialSky = new(SHADER_SKY, IMAGE_SKY);
            _materialSkyLight = new(SHADER_SKY, IMAGE_SKY_LIGHT);
        }

        public Entity Duplicate(Entity refEntity, Entity parent = null)
        {
            Entity clonedEntity = refEntity.Clone();
            clonedEntity.Parent = parent;

            EntityList.Add(clonedEntity);

            return clonedEntity;
        }

        public Entity CreateEntity(Entity parent = null, string name = "New Entity", string tag = "Untagged")
        {
            Entity newEntity = new()
            {
                Name = name,
                Parent = parent,
                Tag = tag,
            };

            EntityList.Add(newEntity);

            return newEntity;
        }

        public Entity CreatePrimitive(EPrimitiveTypes type = EPrimitiveTypes.Cube, Entity parent = null)
        {
            Entity newEntity = new()
            {
                Name = type.ToString().FormatString(),
                Parent = parent,
            };

            newEntity.Name = type.ToString().FormatString();
            newEntity.Parent = parent;

            newEntity.AddComponent(new Mesh(ModelLoader.LoadFilePro(Path.Combine(PATH_PRIMITIVES, type.ToString()) + ".obj")));
            newEntity.GetComponent<Mesh>().Material = _materialDefault;

            EntityList.Add(newEntity);

            return newEntity;
        }

        public Entity CreateCamera(string name = "Camera", string tag = "Untagged", Entity parent = null)
        {
            Entity newEntity = new()
            {
                Name = name,
                Parent = parent,
                Tag = tag,
            };

            newEntity.AddComponent(new Camera());

            EntityList.Add(newEntity);

            return newEntity;
        }

        public void CreateSky()
        {
            Sky = new()
            {
                Name = "Sky",
                Tag = EEditorTags.SceneSky.ToString(),
            };
            Sky.Transform.Scale = new Vector3(-1000, 1000, 1000);

            Sky.AddComponent(new Mesh(ModelLoader.LoadFilePro(Path.Combine(PATH_PRIMITIVES, EPrimitiveTypes.Sphere.ToString()) + ".obj")));
            Sky.GetComponent<Mesh>().Material = _materialSky;

            EntityList.Add(Sky);
        }

        public void SetTheme(bool light) => 
            Sky.GetComponent<Mesh>().Material = light ? _materialSkyLight : _materialSky;

        public Entity GetFromID(Guid id)
        {
            foreach (var entity in EntityList)
                if (entity != null)
                    if (entity.ID == id)
                        return entity;

            return null;
        }

        public Entity GetFromTag(string tag)
        {
            foreach (var entity in EntityList)
                if (entity != null)
                    if (entity.Tag.ToString() == tag)
                        return entity;

            return null;
        }

        public void Destroy(Entity entity)
        {
            entity.RemoveComponents();
            EntityList.Remove(entity);
        }
    }
}
