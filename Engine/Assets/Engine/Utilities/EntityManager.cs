using System;
using System.Numerics;
using System.Collections.Generic;
using Engine.Components;
using Engine.Helper;

namespace Engine.Utilities
{
    public enum EPrimitiveTypes
    {
        CUBE,
        SPHERE,
        PLANE,
        CYLINDER,
        CAPSULE,
        SPECIAL
    }
    class EventList<T> : List<T>
    {
        public event EventHandler OnAdd;
        public new void Add(T item)
        {
            if (null != OnAdd)
                OnAdd(this, null);
            base.Add(item);
        }
    }
    internal class EntityManager
    {
        internal EventList<Entity> list = new EventList<Entity>();
        internal Entity sky;

        Material materialDefault;
        Material materialReflection;
        Material materialSky;
        Mesh meshSphere;
        Mesh meshCube;
        Mesh meshSpecial;

        static readonly string SHADER_LIT = @"Shader\Lit.hlsl";
        static readonly string SHADER_SIMPLELIT = @"Shader\SimpleLit.hlsl";
        static readonly string SHADER_UNLIT = @"Shader\Unlit.hlsl";

        static readonly string IMAGE_DEFAULT = @"Textures\dark.png";
        static readonly string IMAGE_SKY = @"Textures\SkyGradient2.png";

        static readonly string OBJ_SPECIAL = @"Models\Lowpoly_tree_sample.obj";
        static readonly string OBJ_CUBE = @"Models\Cube.obj";
        static readonly string OBJ_SPHERE = @"Models\Sphere.obj";


        internal EntityManager()
        {
            materialDefault = new Material(SHADER_SIMPLELIT, IMAGE_DEFAULT);
            materialReflection = new Material(SHADER_LIT, IMAGE_SKY);
            materialSky = new Material(SHADER_UNLIT, IMAGE_SKY);

            meshSpecial = new Mesh(ModelLoader.LoadFilePro(OBJ_SPECIAL));
            meshCube = new Mesh(ModelLoader.LoadFilePro(OBJ_CUBE));
            meshSphere = new Mesh(ModelLoader.LoadFilePro(OBJ_SPHERE));
        }


        internal Entity Duplicate(Entity _refObject)
        {
            Entity gObject = _refObject.Clone();

            list.Add(gObject);
            return gObject;
        }

        internal Entity CreateEmpty(string _name = "Entity")
        {
            Entity gObject = new Entity()
            {
                name = _name,
                material = materialDefault,
            };

            list.Add(gObject);
            return gObject;
        }

        internal Entity CreatePrimitive(EPrimitiveTypes _type)
        {
            Entity gObject = new Entity();
            gObject.material = materialDefault;

            switch (_type)
            {
                case EPrimitiveTypes.SPECIAL:
                    gObject.mesh = meshSpecial;
                    gObject.name = "special" + list.Count.ToString();
                    break;
                case EPrimitiveTypes.CUBE:
                    gObject.mesh = meshCube;
                    gObject.name = "Cube" + list.Count.ToString();
                    break;
                case EPrimitiveTypes.SPHERE:
                    gObject.mesh = meshSphere;
                    gObject.name = "Sphere" + list.Count.ToString();
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

            list.Add(gObject);
            return gObject;
        }
        internal Entity CreatePrimitive(EPrimitiveTypes _type, Entity _parent)
        {
            var gObject = CreatePrimitive(_type);
            gObject.parent = _parent;

            return gObject;
        }

        internal void CreateSky()
        {
            sky = new Entity()
            {
                name = "Sky",
                mesh = meshSphere,
                material = materialSky,
            };

            sky.transform.scale = new Vector3(-1000, -1000, -1000);
        }
    }
}
