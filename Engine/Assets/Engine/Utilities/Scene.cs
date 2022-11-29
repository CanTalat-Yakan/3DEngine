using System;
using System.Linq;
using System.Numerics;
using Editor.Controller;
using Engine.Components;
using Engine.Editor;

namespace Engine.Utilities
{
    internal class Scene
    {
        public string Profile;

        public CameraComponent Camera = new CameraComponent();
        public CameraController CameraController;
        public EntityManager EntitytManager = new EntityManager();

        private Entity _subParent;
        private Entity _special;

        public void Awake()
        {
            CameraController = new CameraController(Camera);
            Camera.Transform.Position = new Vector3(3, 4, 5);
            Camera.Transform.EulerAngles = new Vector3(35, -150, 0);

            EntitytManager.CreateSky();
        }

        public void Start()
        {
            _special = EntitytManager.CreatePrimitive(EPrimitiveTypes.SPECIAL);
            _special.Transform.Scale *= 0.1f;
            _special.Transform.Position.Y += 0.5f;

            Entity parent = EntitytManager.CreateEntity(null, "Content");

            EntitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).Transform.Position = new Vector3(0, 0, 1);
            EntitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).Transform.Position = new Vector3(0, 0, -3);
            EntitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).Transform.Position = new Vector3(0, 2.5f, 0);
            EntitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).Transform.Position = new Vector3(0, -4, 0);
            EntitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).Transform.Position = new Vector3(2, 0, 0);
            EntitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).Transform.Position = new Vector3(-1, 1, 0);

            _subParent = EntitytManager.CreateEntity(null, "Cubes");
            _subParent.Parent = parent;

            EntitytManager.CreatePrimitive(EPrimitiveTypes.CUBE, _subParent);
        }

        public void Update()
        {
            CameraController.Update();
            Camera.RecreateViewConstants();
            EntitytManager.Sky.Transform.Position = Camera.Transform.Position;

            if (Input.Instance.GetKey(Windows.System.VirtualKey.F, EInputState.DOWN))
                _special.Transform.Position += _special.Transform.Forward;
            if (Input.Instance.GetKey(Windows.System.VirtualKey.G, EInputState.DOWN))
                _special.Transform.Position += _special.Transform.Right;
            if (Input.Instance.GetKey(Windows.System.VirtualKey.V, EInputState.DOWN))
                Camera.Transform.Position += Camera.Transform.Right;

            if (Input.Instance.GetKey(Windows.System.VirtualKey.C, EInputState.DOWN))
            {
                OutputController.Log("Spawned 10 Cubes");

                for (int i = 0; i < 10; i++)
                    EntitytManager.CreatePrimitive(EPrimitiveTypes.CUBE, _subParent).Transform = new TransformComponent
                    {
                        EulerAngles = new Vector3(new Random().Next(1, 360), new Random().Next(1, 360), new Random().Next(1, 360)),
                        Scale = new Vector3(new Random().Next(1, 3), new Random().Next(1, 3), new Random().Next(1, 3))
                    };
            }
        }

        public void LateUpdate()
        {
            Profile = "Objects: " + EntitytManager.EntityList.Count().ToString();

            int vertexCount = 0;
            foreach (var item in EntitytManager.EntityList)
                if (item.IsEnabled && item.Mesh != null)
                    vertexCount += item.Mesh.VertexCount;
            Profile += "\n" + "Vertices: " + vertexCount;
        }

        public void Render()
        {
            foreach (var item in EntitytManager.EntityList)
                if (item.IsEnabled && item.Mesh != null)
                    item.Update_Render();

            EntitytManager.Sky.Update_Render();
        }
    }
}
