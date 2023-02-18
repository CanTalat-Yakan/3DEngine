using System.Linq;

namespace Engine.Editor;

internal class SceneBoot : EditorComponent
{
    public Camera SceneCamera;

    public Entity Cubes;

    public override void OnRegister() =>
        // Register the component with the EditorScriptSystem.
        EditorScriptSystem.Register(this);

    public override void OnAwake()
    {
        // Create a camera entity with the name "Camera" and tag "SceneCamera".
        SceneCamera = SceneManager.Scene.EntitytManager.CreateCamera("Camera", EditorTags.SceneCamera.ToString()).GetComponent<Camera>();
        // Set the camera order to the maximum value.
        SceneCamera.CameraOrder = byte.MaxValue;

        // Add the DeactivateSceneCameraOnPlay and CameraController components to the camera entity.
        SceneCamera.Entity.AddComponent<DeactivateCameraOnPlay>();
        SceneCamera.Entity.AddComponent<CameraController>();

        // Set the initial position and rotation of the camera entity.
        SceneCamera.Entity.Transform.LocalPosition = new(3, 4, 5);
        SceneCamera.Entity.Transform.EulerAngles = new(35, -150, 0);

        // Create a sky entity in the scene.
        SceneManager.Scene.EntitytManager.CreateSky();
    }

    public override void OnStart()
    {
        // Create a camera entity with the name "Camera" and the tag ETags.MainCamera.
        SceneManager.Scene.EntitytManager.CreateCamera("Camera", Tags.MainCamera.ToString());

        // Create a parent entity for all cube entities with the name "Cubes".
        Cubes = SceneManager.Scene.EntitytManager.CreateEntity(null, "Cubes");

        // Create a cube primitive under the Cubes entity.
        SceneManager.Scene.EntitytManager.CreatePrimitive(PrimitiveTypes.Cube, Cubes);
    }

    public override void OnUpdate()
    {
        // Set the skybox's position to the camera's position.
        SceneManager.Scene.EntitytManager.Sky.Transform.LocalPosition = CameraSystem.Components.First().Entity.Transform.Position;

        // Reactivate the SceneCamera after OnUpdate is called from the EditorScriptSystem.
        SceneCamera.IsEnabled = true;

        // Example.
        // Check for the 'C' key press to trigger cube spawning.
        if (Input.GetKey(Windows.System.VirtualKey.C, InputState.Down))
        {
            // Log message indicating that 10 cubes have been spawned.
            Output.Log("Spawned 10 Cubes");

            // Loop to spawn 10 cubes with random attributes.
            for (int i = 0; i < 10; i++)
            {
                // Create a new cube and add it to the Cubes entity.
                var newCube = SceneManager.Scene.EntitytManager.CreatePrimitive(PrimitiveTypes.Cube, Cubes);

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

internal class DeactivateCameraOnPlay : Component, IHide
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
        if (Main.Instance.PlayerControl.PlayMode == PlayMode.Playing)
            // Deactivate the SceneCamera after OnUpdate is called from the ScriptSystem.
            SceneCamera.IsEnabled = false;
    }
}
