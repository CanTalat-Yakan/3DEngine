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
    class MyList<T> : List<T>
    {
        public event EventHandler OnAdd;
        public new void Add(T item) // "new" to avoid compiler-warnings, because we're hiding a method from base-class
        {
            if (null != OnAdd)
                OnAdd(this, null);
            base.Add(item);
        }
    }
    internal class EntityManager
    {
        internal MyList<Entity> m_List = new MyList<Entity>();
        internal Entity m_Sky;

        Material m_materialDefault;
        Material m_materialReflection;
        Material m_materialSky;
        Mesh m_meshSphere;
        Mesh m_meshCube;
        Mesh m_meshSpecial;

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
            m_materialDefault = new Material(SHADER_SIMPLELIT, IMAGE_DEFAULT);
            m_materialReflection = new Material(SHADER_LIT, IMAGE_SKY);
            m_materialSky = new Material(SHADER_UNLIT, IMAGE_SKY);

            m_meshSpecial = new Mesh(ModelLoader.LoadFilePro(OBJ_SPECIAL));
            m_meshCube = new Mesh(ModelLoader.LoadFilePro(OBJ_CUBE));
            m_meshSphere = new Mesh(ModelLoader.LoadFilePro(OBJ_SPHERE));
        }


        internal Entity Duplicate(Entity _refObject)
        {
            Entity gObject = _refObject.Clone();

            m_List.Add(gObject);
            return gObject;
        }

        internal Entity CreateEmpty(string _name = "Entity")
        {
            Entity gObject = new Entity()
            {
                m_Name = _name,
                m_Material = m_materialDefault,
            };

            m_List.Add(gObject);
            return gObject;
        }

        internal Entity CreatePrimitive(EPrimitiveTypes _type)
        {
            Entity gObject = new Entity();
            gObject.m_Material = m_materialDefault;

            switch (_type)
            {
                case EPrimitiveTypes.SPECIAL:
                    gObject.m_Mesh = m_meshSpecial;
                    gObject.m_Name = "special" + m_List.Count.ToString();
                    break;
                case EPrimitiveTypes.CUBE:
                    gObject.m_Mesh = m_meshCube;
                    gObject.m_Name = "Cube" + m_List.Count.ToString();
                    break;
                case EPrimitiveTypes.SPHERE:
                    gObject.m_Mesh = m_meshSphere;
                    gObject.m_Name = "Sphere" + m_List.Count.ToString();
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

            m_List.Add(gObject);
            return gObject;
        }
        internal Entity CreatePrimitive(EPrimitiveTypes _type, Entity _parent)
        {
            var gObject = CreatePrimitive(_type);
            gObject.m_Parent = _parent;

            return gObject;
        }

        internal void CreateSky()
        {
            m_Sky = new Entity()
            {
                m_Name = "Sky",
                m_Mesh = m_meshSphere,
                m_Material = m_materialSky,
            };

            m_Sky.m_Transform.m_Scale = new Vector3(-1000, -1000, -1000);
        }
    }
}
