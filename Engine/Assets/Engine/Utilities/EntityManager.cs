using System;
using System.Numerics;
using System.Collections.Generic;
using Engine.Components;
using Engine.Helper;

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
        public event EventHandler EventOnAdd;

        public new void Add(T item)
        {
            if (null != EventOnAdd)
                EventOnAdd(this, null);

            base.Add(item);
        }
    }

    internal class EntityManager
    {
        public EventList<Entity> EntityList = new EventList<Entity>();
        public Entity Sky;

        private MaterialComponent _materialDefault;
        private MaterialComponent _materialReflection;
        private MaterialComponent _materialSky;
        private MeshComponent _meshSphere;
        private MeshComponent _meshCube;
        private MeshComponent _meshSpecial;

        static readonly string SHADER_LIT = @"Shader\Lit.hlsl";
        static readonly string SHADER_SIMPLELIT = @"Shader\SimpleLit.hlsl";
        static readonly string SHADER_UNLIT = @"Shader\Unlit.hlsl";

        static readonly string IMAGE_DEFAULT = @"Textures\dark.png";
        static readonly string IMAGE_SKY = @"Textures\SkyGradient2.png";

        static readonly string OBJ_SPECIAL = @"Models\Lowpoly_tree_sample.obj";
        static readonly string OBJ_CUBE = @"Models\Cube.obj";
        static readonly string OBJ_SPHERE = @"Models\Sphere.obj";

        public EntityManager()
        {
            _materialDefault = new MaterialComponent(SHADER_SIMPLELIT, IMAGE_DEFAULT);
            _materialReflection = new MaterialComponent(SHADER_LIT, IMAGE_SKY);
            _materialSky = new MaterialComponent(SHADER_UNLIT, IMAGE_SKY);

            _meshSpecial = new MeshComponent(ModelLoader.LoadFilePro(OBJ_SPECIAL));
            _meshCube = new MeshComponent(ModelLoader.LoadFilePro(OBJ_CUBE));
            _meshSphere = new MeshComponent(ModelLoader.LoadFilePro(OBJ_SPHERE));
        }

        public Entity Duplicate(Entity refEntity)
        {
            Entity gObject = refEntity.Clone();

            EntityList.Add(gObject);
            return gObject;
        }

        public Entity CreateEmpty(string name = "Entity")
        {
            Entity gObject = new Entity()
            {
                Name = name,
                Material = _materialDefault,
            };

            EntityList.Add(gObject);
            return gObject;
        }

        public Entity CreatePrimitive(EPrimitiveTypes type)
        {
            Entity gObject = new Entity();
            gObject.Material = _materialDefault;

            switch (type)
            {
                case EPrimitiveTypes.SPECIAL:
                    gObject.Mesh = _meshSpecial;
                    gObject.Name = "special" + EntityList.Count.ToString();
                    break;
                case EPrimitiveTypes.CUBE:
                    gObject.Mesh = _meshCube;
                    gObject.Name = "Cube" + EntityList.Count.ToString();
                    break;
                case EPrimitiveTypes.SPHERE:
                    gObject.Mesh = _meshSphere;
                    gObject.Name = "Sphere" + EntityList.Count.ToString();
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

            EntityList.Add(gObject);
            return gObject;
        }

        public Entity CreatePrimitive(EPrimitiveTypes type, Entity parent)
        {
            var gObject = CreatePrimitive(type);
            gObject.Parent = parent;

            return gObject;
        }

        public void CreateSky()
        {
            Sky = new Entity()
            {
                Name = "Sky",
                Mesh = _meshSphere,
                Material = _materialSky,
            };

            Sky.Transform.Scale = new Vector3(-1000, -1000, -1000);
        }
    }
}
