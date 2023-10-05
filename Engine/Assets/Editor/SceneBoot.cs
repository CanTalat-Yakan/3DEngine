namespace Engine.Editor;

internal sealed class SceneBoot : EditorComponent
{
    public Camera SceneCamera;

    public Entity DefaultSky;

    public Entity Cubes;

    public override void OnAwake()
    {
        // Create a camera entity with the name "Camera" and tag "SceneCamera".
        SceneCamera = SceneManager.MainScene.EntityManager.CreateCamera("Camera", EditorTags.SceneCamera.ToString());
        // Set the camera order to the maximum value.
        SceneCamera.CameraID = byte.MaxValue;

        // Add the DeactivateSceneCameraOnPlay and CameraController components to the camera entity.
        SceneCamera.Entity.AddComponent<DeactivateCameraOnPlay>();
        SceneCamera.Entity.AddComponent<ViewportController>().SetCamera(SceneCamera);

        // Set the initial position and rotation of the camera entity.
        SceneCamera.Entity.Transform.LocalPosition = new(3, 4, 5);
        SceneCamera.Entity.Transform.EulerAngles = new(35, -150, 0);

        // Create a sky entity in the scene.
        DefaultSky = SceneManager.MainScene.EntityManager
            .CreateEntity()
            .AddComponent<DefaultSky>().Entity;
    }

    public override void OnStart()
    {
        // Create a camera entity with the name "Camera" and the tag ETags.MainCamera.
        SceneManager.MainScene.EntityManager.CreateCamera("Camera", Tags.MainCamera.ToString());

        // Create a parent entity for all cube entities with the name "Cubes".
        Cubes = SceneManager.MainScene.EntityManager.CreateEntity(null, "Cubes");

        // Create a cube primitive under the Cubes entity.
        SceneManager.MainScene.EntityManager.CreatePrimitive(PrimitiveTypes.Cube, Cubes);
    }

    public override void OnUpdate()
    {
        // Reactivate the SceneCamera after OnUpdate is called from the EditorScriptSystem.
        SceneCamera.IsEnabled = true;

        // Example.
        // Check for the 'C' key press to trigger cube spawning.
        if (Input.GetKey(Key.C, InputState.Down) && ViewportController.ViewportFocused)
        {
            // Log message indicating that 10 cubes have been spawned.
            Output.Log("Spawned 100 Cubes");

            // Loop to spawn 100 cubes with random attributes.
            for (int i = 0; i < 100; i++)
            {
                // Create a new cube and add it to the Cubes entity.
                var newCube = SceneManager.MainScene.EntityManager.CreatePrimitive(PrimitiveTypes.Cube, Cubes);

                // Set the position of the new cube with an offset on the Y axis.
                newCube.Entity.Transform.LocalPosition.Y -= 3;
                // Set random rotation values for the new cube.
                newCube.Entity.Transform.EulerAngles = new(new Random().Next(1, 360), new Random().Next(1, 360), new Random().Next(1, 360));
                // Set random scale values for the new cube.
                newCube.Entity.Transform.LocalScale = new(new Random().Next(1, 3), new Random().Next(1, 3), new Random().Next(1, 3));
            }
        }
    }
}

internal sealed class DeactivateCameraOnPlay : Component, IHide
{
    public Camera SceneCamera;

    public override void OnAwake() =>
        // Get the SceneCamera component from the entity with the tag "SceneCamera".
        SceneCamera = Entity.GetComponent<Camera>();

    public override void OnUpdate()
    {
        // Check if the play mode is set to "Playing" before deactivating the SceneCamera.
        if (EditorState.PlayMode)
            // Deactivate the SceneCamera after OnUpdate is called from the ScriptSystem.
            SceneCamera.IsEnabled = false;
    }
}
