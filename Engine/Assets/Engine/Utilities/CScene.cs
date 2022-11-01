using Assimp;
using System;
using System.Linq;
using System.Numerics;
using Editor.Controls;
using Engine.Components;
using Engine.Editor;

namespace Engine.Utilities
{
    internal class CScene
    {
        internal string m_Profile;

        internal CCamera m_Camera = new CCamera();
        internal CController m_CameraController;
        internal CObjectManager m_ObjectManager = new CObjectManager();

        internal void Awake()
        {
            m_CameraController = new CController(m_Camera);
            m_Camera.m_Transform.m_Position = new Vector3(3, 4, 5);
            m_Camera.m_Transform.m_EulerAngles = new Vector3(35, -150, 0);

            m_ObjectManager.CreateSky();
        }

        CObject subParent;
        CObject special;
        internal void Start()
        {
            special = m_ObjectManager.CreatePrimitive(EPrimitiveTypes.SPECIAL);
            special.m_Transform.m_Scale *= 0.1f;
            special.m_Transform.m_Position.Y += 0.5f;

            CObject parent = m_ObjectManager.CreateEmpty("Content");
            subParent = m_ObjectManager.CreateEmpty("Cubes");
            subParent.m_Parent = parent;

            m_ObjectManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).m_Transform.m_Position = new Vector3(0, 0, 1);
            m_ObjectManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).m_Transform.m_Position = new Vector3(0, 0, -3);
            m_ObjectManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).m_Transform.m_Position = new Vector3(0, 2.5f, 0);
            m_ObjectManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).m_Transform.m_Position = new Vector3(0, -4, 0);
            m_ObjectManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).m_Transform.m_Position = new Vector3(2, 0, 0);
            m_ObjectManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).m_Transform.m_Position = new Vector3(-1, 1, 0);
            m_ObjectManager.CreatePrimitive(EPrimitiveTypes.CUBE, subParent);
        }

        internal void Update()
        {
            m_CameraController.Update();
            m_Camera.RecreateViewConstants();
            m_ObjectManager.m_Sky.m_Transform.m_Position = m_Camera.m_Transform.m_Position;

            if (CInput.Instance.GetKey(Windows.System.VirtualKey.F, CInput.EInputState.DOWN))
                special.m_Transform.m_Position += special.m_Transform.Forward;
            if (CInput.Instance.GetKey(Windows.System.VirtualKey.G, CInput.EInputState.DOWN))
                special.m_Transform.m_Position += special.m_Transform.Right;
            if (CInput.Instance.GetKey(Windows.System.VirtualKey.V, CInput.EInputState.DOWN))
                m_Camera.m_Transform.m_Position += m_Camera.m_Transform.Right;

            if (CInput.Instance.GetKey(Windows.System.VirtualKey.C, CInput.EInputState.DOWN))
            {
                COutput.Log("Spawned Cube");

                m_ObjectManager.CreatePrimitive(EPrimitiveTypes.CUBE, subParent).m_Transform = new CTransform
                {
                    m_EulerAngles = new Vector3(new Random().Next(1, 360), new Random().Next(1, 360), new Random().Next(1, 360)),
                    m_Scale = new Vector3(new Random().Next(1, 3), new Random().Next(1, 3), new Random().Next(1, 3))
                };
            }
        }

        internal void LateUpdate()
        {
            m_Profile = "Objects: " + m_ObjectManager.m_List.Count().ToString();

            int vertexCount = 0;
            foreach (var item in m_ObjectManager.m_List)
                if (item.m_Enabled && item.m_Mesh != null)
                    vertexCount += item.m_Mesh.m_VertexCount;
            m_Profile += "\n" + "Vertices: " + vertexCount;
        }

        internal void Render()
        {
            foreach (var item in m_ObjectManager.m_List)
                if (item.m_Enabled && item.m_Mesh != null)
                    item.Update_Render();

            m_ObjectManager.m_Sky.Update_Render();
        }
    }
}
