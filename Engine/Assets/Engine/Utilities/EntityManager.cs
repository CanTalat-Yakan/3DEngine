using System.Numerics;
using System;
using System.Collections.Generic;
using Engine.Components;
using Engine.Helper;
using Engine.ECS;
using System.Xml.Linq;

namespace Engine.Utilities
{
    internal enum EPrimitiveTypes
    {
        CUBE,
        SPHERE,
        PLANE,
        CYLINDER,
        CAPSULE,
        SPECIAL
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

        private static readonly string OBJ_SPECIAL = @"Models\Lowpoly_tree_sample.obj";
        private static readonly string OBJ_CUBE = @"Models\Cube.obj";
        private static readonly string OBJ_SPHERE = @"Models\Sphere.obj";

        public EntityManager()
        {
            _materialDefault = new Material(SHADER_SIMPLELIT, IMAGE_DEFAULT);
            _materialReflection = new Material(SHADER_LIT, IMAGE_SKY);
            _materialSky = new Material(SHADER_UNLIT, IMAGE_SKY);
            _materialSkyLight = new Material(SHADER_UNLIT, IMAGE_SKY_LIGHT);
        }

        public Entity Duplicate(Entity refEntity, Entity parent = null)
        {
            Entity gObject = refEntity.Clone();
            gObject.Parent = parent;

            EntityList.Add(gObject);

            return gObject;
        }

        public Entity CreateEntity(Entity parent = null, string name = "New Entity")
        {
            Entity newEntity = new Entity()
            {
                Name = name,
                Parent = parent
            };

            EntityList.Add(newEntity);

            return newEntity;
        }

        public Entity CreatePrimitive(EPrimitiveTypes type, Entity parent = null)
        {
            Entity newEntity = new Entity();
            newEntity.Parent = parent;

            switch (type)
            {
                case EPrimitiveTypes.SPECIAL:
                    newEntity.Name = "special";
                    newEntity.AddComponent(new Mesh(ModelLoader.LoadFilePro(OBJ_SPECIAL)));
                    break;
                case EPrimitiveTypes.CUBE:
                    newEntity.Name = "Cube";
                    newEntity.AddComponent(new Mesh(ModelLoader.LoadFilePro(OBJ_CUBE)));
                    break;
                case EPrimitiveTypes.SPHERE:
                    newEntity.Name = "Sphere";
                    newEntity.AddComponent(new Mesh(ModelLoader.LoadFilePro(OBJ_SPHERE)));
                    break;
                case EPrimitiveTypes.PLANE:
                    break;
                case EPrimitiveTypes.CYLINDER:
                    break;
                case EPrimitiveTypes.CAPSULE:
                    break;
                default:
                    break;
            }

            newEntity.GetComponent<Mesh>().Material = _materialDefault;

            EntityList.Add(newEntity);

            return newEntity;
        }

        public void CreateSky()
        {
            Sky = new Entity() { Name = "Sky" };
            Sky.AddComponent(new Mesh(ModelLoader.LoadFilePro(OBJ_SPHERE)));
            Sky.GetComponent<Mesh>().Material = _materialSky;

            Sky.Transform.Scale = new Vector3(-1000, -1000, -1000);
        }

        public Entity GetFromID(Guid id)
        {
            foreach (var entity in EntityList)
                if (entity.ID == id)
                    return entity;

            return null;
        }

        public void SetTheme(bool light) => Sky.GetComponent<Mesh>().Material = light ? _materialSkyLight : _materialSky;

        public void Destroy(Entity sourceEntity) => EntityList.Remove(sourceEntity);
    }
}
