using System.Drawing;
using System.Numerics;
using System;
using Engine.Components;
using Editor.Controller;
using Engine.ECS;
using Engine.Utilities;
using Texture = Vortice.Direct3D11.Texture2DArrayShaderResourceView;

namespace Engine.Editor
{
    internal class SceneBoot : EditorComponent
    {
        public Entity Camera;
        public Entity Cubes;

        public CameraController CameraController;


        public override void OnRegister() => 
            EditorScriptSystem.Register(this);

        public override void OnAwake()
        {
            Camera = SceneManager.Scene.EntitytManager.CreateEntity(null, "Camera");
            Camera.Tag = ETags.MainCamera;
            Camera.Transform.Position = new(3, 4, 5);
            Camera.Transform.EulerAngles = new(35, -150, 0);
            Camera.AddComponent(new Camera());
            Camera.AddComponent(new CameraController());

            SceneManager.Scene.EntitytManager.CreateSky();
        }

        public override void OnStart()
        {
            var tree = SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Tree);
            tree.Transform.Position.Y += 0.5f;
            tree.AddComponent(new PlayerMovement());
            tree.AddComponent(new Test());

            Cubes = SceneManager.Scene.EntitytManager.CreateEntity(null, "Cubes");

            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Cube, Cubes);
        }

        public override void OnUpdate()
        {
            SceneManager.Scene.EntitytManager.Sky.Transform.Position = Camera.Transform.Position;

            if (Input.Instance.GetKey(Windows.System.VirtualKey.C, EInputState.Down))
            {
                Output.Log("Spawned 10 Cubes");

                for (int i = 0; i < 10; i++)
                {
                    var newCube = SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Cube, Cubes);
                    newCube.Transform.Position.Y -= 3;
                    newCube.Transform.EulerAngles = new(new Random().Next(1, 360), new Random().Next(1, 360), new Random().Next(1, 360));
                    newCube.Transform.Scale = new(new Random().Next(1, 3), new Random().Next(1, 3), new Random().Next(1, 3));
                }
            }
        }
    }

    internal class PlayerMovement : Component
    {
        public float MovementSpeed = 5;

        public override void OnRegister() => 
            ScriptSystem.Register(this);

        public override void OnUpdate()
        {
            Vector3 targetDirection = Movement();

            if (!IsNaN(targetDirection))
                _entity.Transform.Position += targetDirection;

            if (Input.Instance.GetKey(Windows.System.VirtualKey.F, EInputState.Down))
                _entity.GetComponent<Mesh>().IsActive = !_entity.GetComponent<Mesh>().IsActive;
        }

        internal Vector3 Movement()
        {
            Vector3 dest =
                Input.Instance.GetAxis().X * _entity.Transform.Right +
                Input.Instance.GetAxis().Y * _entity.Transform.Forward;

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
        private string _visibleString = "This field is private!";
        [Hide]
        public string HiddenString = "This field is public!";
        public Color Color;
        public string String = "";
        public int Int;
        public float Float;
        public Vector2 Vector2;
        public Vector3 Vector3;
        [Slider(1, 100)]
        public float Slider;
        public bool Bool;
        public Texture Texture;
        public Entity Entity;
        [Spacer]
        [Header("Header")]
        public event EventHandler Event;

        public override void OnRegister() => 
            ScriptSystem.Register(this);
    }
}
