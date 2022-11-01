using Assimp;
using System;
using System.Linq;
using System.Numerics;
using Editor.Controls;
using Engine.Components;
using Engine.Editor;

namespace Engine.Utilities
{
    internal class Scene
    {
        internal string profile;

        internal Components.Camera camera = new Components.Camera();
        internal Controller cameraController;
        internal EntityManager entitytManager = new EntityManager();

        internal void Awake()
        {
            cameraController = new Controller(camera);
            camera.transform.position = new Vector3(3, 4, 5);
            camera.transform.eulerAngles = new Vector3(35, -150, 0);

            entitytManager.CreateSky();
        }

        Entity subParent;
        Entity special;
        internal void Start()
        {
            special = entitytManager.CreatePrimitive(EPrimitiveTypes.SPECIAL);
            special.transform.scale *= 0.1f;
            special.transform.position.Y += 0.5f;

            Entity parent = entitytManager.CreateEmpty("Content");
            subParent = entitytManager.CreateEmpty("Cubes");
            subParent.parent = parent;

            entitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).transform.position = new Vector3(0, 0, 1);
            entitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).transform.position = new Vector3(0, 0, -3);
            entitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).transform.position = new Vector3(0, 2.5f, 0);
            entitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).transform.position = new Vector3(0, -4, 0);
            entitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).transform.position = new Vector3(2, 0, 0);
            entitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).transform.position = new Vector3(-1, 1, 0);
            entitytManager.CreatePrimitive(EPrimitiveTypes.CUBE, subParent);
        }

        internal void Update()
        {
            cameraController.Update();
            camera.RecreateViewConstants();
            entitytManager.sky.transform.position = camera.transform.position;

            if (Input.Instance.GetKey(Windows.System.VirtualKey.F, Input.EInputState.DOWN))
                special.transform.position += special.transform.forward;
            if (Input.Instance.GetKey(Windows.System.VirtualKey.G, Input.EInputState.DOWN))
                special.transform.position += special.transform.right;
            if (Input.Instance.GetKey(Windows.System.VirtualKey.V, Input.EInputState.DOWN))
                camera.transform.position += camera.transform.right;

            if (Input.Instance.GetKey(Windows.System.VirtualKey.C, Input.EInputState.DOWN))
            {
                OutputController.Log("Spawned Cube");

                entitytManager.CreatePrimitive(EPrimitiveTypes.CUBE, subParent).transform = new Transform
                {
                    eulerAngles = new Vector3(new Random().Next(1, 360), new Random().Next(1, 360), new Random().Next(1, 360)),
                    scale = new Vector3(new Random().Next(1, 3), new Random().Next(1, 3), new Random().Next(1, 3))
                };
            }
        }

        internal void LateUpdate()
        {
            profile = "Objects: " + entitytManager.list.Count().ToString();

            int vertexCount = 0;
            foreach (var item in entitytManager.list)
                if (item.isEnabled && item.mesh != null)
                    vertexCount += item.mesh.vertexCount;
            profile += "\n" + "Vertices: " + vertexCount;
        }

        internal void Render()
        {
            foreach (var item in entitytManager.list)
                if (item.isEnabled && item.mesh != null)
                    item.Update_Render();

            entitytManager.sky.Update_Render();
        }
    }
}
