using System.Drawing;
using System.Numerics;
using System.Linq;
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

        public Entity Cubes;

        public override void OnRegister() =>
            // Register the component with the EditorScriptSystem.
            EditorScriptSystem.Register(this);

        public override void OnAwake()
        {
            // Create a camera entity with the name "Camera" and tag "SceneCamera".
            SceneCamera = SceneManager.Scene.EntitytManager.CreateCamera("Camera", EEditorTags.SceneCamera.ToString()).GetComponent<Camera>();
            // Set the camera order to the maximum value.
            SceneCamera.CameraOrder = byte.MaxValue;

            // Add the DeactivateSceneCameraOnPlay and CameraController components to the camera entity.
            SceneCamera.Entity.AddComponent<DeactivateOnPlay>();
            SceneCamera.Entity.AddComponent<CameraController>();

            // Set the initial position and rotation of the camera entity.
            SceneCamera.Entity.Transform.LocalPosition = new(3, 4, 5);
            SceneCamera.Entity.Transform.EulerAngles = new(35, -150, 0);

            // Create a sky entity in the scene.
            SceneManager.Scene.EntitytManager.CreateSky();
        }

        public override void OnStart()
        {
            // Create a player entity with the name "Player" and the tag ETags.Player.
            var player = SceneManager.Scene.EntitytManager.CreateEntity(null, "Player", ETags.Player.ToString());
            player.Transform.LocalPosition.Z -= 2;
            // Add PlayerMovement and Example component to the player entity.
            player.AddComponent<PlayerMovement>();
            player.AddComponent<Example>();

            // Create a camera entity with the name "Camera" and the tag ETags.MainCamera.
            SceneManager.Scene.EntitytManager.CreateCamera("Camera", ETags.MainCamera.ToString(), player);

            // Create a parent entity for all cube entities with the name "Cubes".
            Cubes = SceneManager.Scene.EntitytManager.CreateEntity(null, "Cubes");

            // Create a cube primitive under the Cubes entity.
            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Cube, Cubes);
        }
        
        public override void OnUpdate()
        {
            // Set the skybox's position to the camera's position.
            SceneManager.Scene.EntitytManager.Sky.Transform.LocalPosition = CameraSystem.Components.First().Entity.Transform.Position;

            // Reactivate the SceneCamera after OnUpdate is called from the EditorScriptSystem.
            SceneCamera.IsEnabled = true;

            // Example.
            // Check for the 'C' key press to trigger cube spawning.
            if (Input.GetKey(Windows.System.VirtualKey.C, EInputState.Down))
            {
                // Log message indicating that 10 cubes have been spawned.
                Output.Log("Spawned 10 Cubes");

                // Loop to spawn 10 cubes with random attributes.
                for (int i = 0; i < 10; i++)
                {
                    // Create a new cube and add it to the Cubes entity.
                    var newCube = SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.Cube, Cubes);

                    // Set the position of the new cube with an offset on the Y axis.
                    newCube.Transform.LocalPosition.Y -= 3;
                    // Set random rotation values for the new cube.
                    newCube.Transform.EulerAngles = new(new Random().Next(1, 360), new Random().Next(1, 360), new Random().Next(1, 360));
                    // Set random scale values for the new cube.
                    newCube.Transform.LocalScale = new(new Random().Next(1, 3), new Random().Next(1, 3), new Random().Next(1, 3));
                }
            }
        }
    }

    internal class DeactivateOnPlay : Component, IHide
    {
        public Camera SceneCamera;

        public override void OnRegister() =>
            // Register the component with the ScriptSystem.
            ScriptSystem.Register(this);

        public override void OnAwake() =>
            // Get the SceneCamera component from the entity with the tag "SceneCamera".
            SceneCamera = Entity.GetComponent<Camera>();

        public override void OnUpdate()
        {
            // Check if the playmode is set to "Playing" before deactivating the SceneCamera.
            if (Main.Instance.PlayerControl.Playmode == EPlaymode.Playing)
                // Deactivate the SceneCamera after OnUpdate is called from the ScriptSystem.
                SceneCamera.IsEnabled = false;
        }
    }

    internal class PlayerMovement : Component
    {
        public float MovementSpeed = 2;
        public float RotationSpeed = 5;

        private Vector3 _targetDirection;
        private Vector2 _cameraRotataion;

        public override void OnRegister() =>
            // Register the component with the ScriptSystem.
            ScriptSystem.Register(this);

        public override void OnUpdate()
        {
            // Compute the target direction and the camera rotation.
            Movement();
            Rotation();

            // Check if the target direction is not NaN.
            if (!_targetDirection.IsNaN())
                // Add the target direction to the entity's position.
                Entity.Transform.LocalPosition += _targetDirection;
            // Add the horizontal camera rotation to the entity's rotation.
            Entity.Transform.EulerAngles = Vector3.UnitY * _cameraRotataion.Y;

            // Limit the camera's vertical rotation between -89 and 89 degrees.
            _cameraRotataion.X = Math.Clamp(_cameraRotataion.X, -89, 89);
            // Add the vertical camera rotation to the main camera.
            Camera.Main.Entity.Transform.EulerAngles = Vector3.UnitX * _cameraRotataion.X;
        }

        internal void Movement()
        {
            // Calculate the destination position based on the input axis values.
            Vector3 destination =
                Input.GetAxis().X * Entity.Transform.Right +
                Input.GetAxis().Y * Entity.Transform.Forward;

            // Return the normalized movement direction with a magnitude of MovementSpeed multiplied by the delta time.
            _targetDirection = Vector3.Normalize(destination) * MovementSpeed * (float)Timer.Delta;
        }

        internal void Rotation()
        {
            if (!Input.GetButton(EMouseButton.IsRightButtonPressed))
                return;
            
            // Create a new rotation based on the mouse X and Y axis inputs.
            Vector2 rotation = new(
                Input.GetMouseAxis().Y, 
                Input.GetMouseAxis().X);

            // Update the entity's rotation based on the calculated rotation and rotation speed.
            _cameraRotataion -= rotation * (float)Timer.Delta * RotationSpeed;
        }
    }

    internal class Example : Component
    {
        [ToolTip("This is a ToolTip")]
        [Show]
        private string _visibleString = "This field is private";
        [Hide]
        public string HiddenString = "This field is public";
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
        public Entity _Entity;
        [Spacer]
        [Header("Header")]
        public event EventHandler Event;

        public override void OnRegister() =>
            ScriptSystem.Register(this);
    }
}
