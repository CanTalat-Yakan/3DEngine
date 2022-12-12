using System.Drawing;
using System.Numerics;
using System;
using Engine.Components;
using Editor.Controller;
using Engine.ECS;
using Engine.Utilities;
using Texture = Vortice.Direct3D11.Texture2DArrayShaderResourceView;
//using Microsoft.CodeAnalysis;

namespace Engine.Editor
{
    //[Generator]
    internal class SceneBoot : EditorComponent
    {
        public Entity Camera;

        public CameraController CameraController;

        private Entity _subParent;
        private Entity _special;

        public override void Register() => EditorScriptSystem.Register(this);

        public override void Awake()
        {
            Camera = SceneManager.Scene.EntitytManager.CreateEntity(null, "Camera");
            Camera.Tag = ETags.MainCamera;
            Camera.Transform.Position = new(3, 4, 5);
            Camera.Transform.EulerAngles = new(35, -150, 0);
            Camera.AddComponent(new Camera());
            Camera.AddComponent(new CameraController());

            SceneManager.Scene.EntitytManager.CreateSky();
        }

        public override void Start()
        {
            _special = SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Special);
            _special.Transform.Scale *= 0.1f;
            _special.Transform.Position.Y += 0.5f;
            _special.AddComponent(new PlayerMovement());
            _special.AddComponent(new Test());

            Entity parent = SceneManager.Scene.EntitytManager.CreateEntity(null, "Content");

            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Sphere, parent).Transform.Position = new(0, 0, 1);
            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Sphere, parent).Transform.Position = new(0, 0, -3);
            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Sphere, parent).Transform.Position = new(0, 2.5f, 0);
            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Sphere, parent).Transform.Position = new(0, -4, 0);
            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Sphere, parent).Transform.Position = new(2, 0, 0);
            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Sphere, parent).Transform.Position = new(-1, 1, 0);

            _subParent = SceneManager.Scene.EntitytManager.CreateEntity(null, "Cubes");
            _subParent.Parent = parent;

            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Cube, _subParent);
        }

        public override void Update()
        {
            SceneManager.Scene.EntitytManager.Sky.Transform.Position = Camera.Transform.Position;

            if (Input.Instance.GetKey(Windows.System.VirtualKey.C, EInputState.DOWN))
            {
                Output.Log("Spawned 10 Cubes");

                for (int i = 0; i < 10; i++)
                {
                    var newCube = SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Cube, _subParent);
                    newCube.Transform.EulerAngles = new(new Random().Next(1, 360), new Random().Next(1, 360), new Random().Next(1, 360));
                    newCube.Transform.Scale = new(new Random().Next(1, 3), new Random().Next(1, 3), new Random().Next(1, 3));
                }
            }
        }
    }

    internal class PlayerMovement : Component
    {
        public float MovementSpeed = 5;

        public override void Register() => ScriptSystem.Register(this);

        public override void Update()
        {
            Vector3 targetDirection = Movement();

            if (!IsNaN(targetDirection))
                entity.Transform.Position += targetDirection;
        }

        internal Vector3 Movement()
        {
            Vector3 dest =
                Input.Instance.GetAxis().X * entity.Transform.Right +
                Input.Instance.GetAxis().Y * entity.Transform.Forward;

            return Vector3.Normalize(dest) * MovementSpeed * (float)Time.Delta;
        }

        bool IsNaN(Vector3 vec)
        {
            if (!float.IsNaN(vec.X))
                if (!float.IsNaN(vec.Y))
                    if (!float.IsNaN(vec.Z))
                        return false;

            return true;
        }
    }

    internal class Test : Component
    {
        [Show]
        private string _visibleString = "this is a private field";
        [Hide]
        private string HiddenString = "this is a public field";
        public Color Color;
        public string String = "";
        public int Int;
        public float Float;
        public Vector2 Vector2;
        public Vector3 Vector3;
        [Slider(1, 10)]
        public float Slider;
        public bool Bool;
        public Texture Texture;
        public Entity Entity;
        [Spacer]
        [Header("Header")]
        public event EventHandler Event;

        public override void Register() => ScriptSystem.Register(this);
    }
}
