using System.Drawing;
using System.Numerics;
using System;
using Editor.Controller;
using Engine.Components;
using Engine.ECS;
using Engine.Utilities;
using Texture = Vortice.Direct3D11.Texture2DArrayShaderResourceView;

namespace Engine.Editor
{
    internal class SceneBoot : EditorComponent
    {
        public Camera SceneCamera;
        public CameraController CameraController;

        public Entity Camera;
        public Entity Cubes;

        public override void OnRegister() =>
            EditorScriptSystem.Register(this);

        public override void OnAwake()
        {
            SceneCamera = SceneManager.Scene.EntitytManager.CreateCamera("Camera", EEditorTags.SceneCamera.ToString()).GetComponent<Camera>();
            SceneCamera.Order = byte.MaxValue;

            SceneCamera.Entity.AddComponent(new DeactivateSceneCameraOnPlay());
            SceneCamera.Entity.AddComponent(new CameraController());

            SceneCamera.Entity.Transform.Position = new(3, 4, 5);
            SceneCamera.Entity.Transform.EulerAngles = new(35, -150, 0);

            SceneManager.Scene.EntitytManager.CreateSky();
        }

        public override void OnStart()
        {
            var tree = SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Tree);
            tree.Transform.Position.Y += 0.5f;
            tree.AddComponent(new PlayerMovement());
            tree.AddComponent(new Test());

            Camera = SceneManager.Scene.EntitytManager.CreateCamera("Camera", ETags.MainCamera.ToString(), tree);

            Cubes = SceneManager.Scene.EntitytManager.CreateEntity(null, "Cubes");
            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Cube, Cubes);
        }

        public override void OnUpdate()
        {
            SceneManager.Scene.EntitytManager.Sky.Transform.Position = Camera.Transform.Position;

            if (Input.GetKey(Windows.System.VirtualKey.C, EInputState.Down))
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

            SceneCamera.IsEnabled = true;
        }
    }

    internal class DeactivateSceneCameraOnPlay : Component
    {
        public Camera SceneCamera;

        public override void OnRegister() =>
            ScriptSystem.Register(this);

        public override void OnAwake() =>
            SceneCamera = SceneManager.Scene.EntitytManager.GetFromTag("SceneCamera").GetComponent<Camera>();

        public override void OnUpdate()
        {
            if (Main.Instance.ControlPlayer.Playmode == EPlaymode.Playing)
                SceneCamera.IsEnabled = false;
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

            if (!targetDirection.IsNaN())
                Entity.Transform.Position += targetDirection;
        }

        internal Vector3 Movement()
        {
            Vector3 dest =
                Input.GetAxis().X * Entity.Transform.Right +
                Input.GetAxis().Y * Entity.Transform.Forward;

            return Vector3.Normalize(dest) * MovementSpeed * (float)Time.Delta;
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
