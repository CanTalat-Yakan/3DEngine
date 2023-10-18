namespace Engine.Editor;

internal sealed class SceneBoot : EditorComponent, IHide
{
    public Camera SceneCamera;

    public Entity DefaultSky;
    public Entity Cubes;

    public int CubesCount;

    public override void OnAwake()
    {
        // Create a camera entity with the name "Camera" and tag "SceneCamera".
        SceneCamera = SceneManager.MainScene.EntityManager.CreateCamera("Camera");
        SceneCamera.Entity.IsHidden = true;
        // Set the camera order to the maximum value.
        SceneCamera.CameraID = byte.MaxValue;

        // Add the DeactivateSceneCameraOnPlay and CameraController components to the camera entity.
        SceneCamera.Entity.AddComponent<ViewportController>().SetCamera(SceneCamera);

        // Set the initial position and rotation of the camera entity.
        SceneCamera.Entity.Transform.LocalPosition = new(3, 4, 5);
        SceneCamera.Entity.Transform.EulerAngles = new(35, -150, 0);

        // Create a sky entity in the scene.
        var defaultSky = SceneManager.MainScene.EntityManager
            .CreateEntity()
            .AddComponent<DefaultSky>();
        defaultSky.Initialize();

        DefaultSky = defaultSky.Entity;
    }

    public override void OnStart()
    {
        // Create a camera entity with the name "Camera" and the tag ETags.MainCamera.
        SceneManager.MainScene.EntityManager.CreateCamera("Camera", Tags.MainCamera.ToString());

        // Create a parent entity for all cube entities with the name "Cubes".
        Cubes = SceneManager.MainScene.EntityManager.CreateEntity(null, "Cubes");

        // Create a cube primitive under the Cubes entity.
        SceneManager.MainScene.EntityManager.CreatePrimitive(PrimitiveTypes.Cube, Cubes);

        Output.Log("Press 'C' to spawn 1000 Cubes");
    }

    public override void OnUpdate()
    {
        // Deactivate the SceneCamera when the play mode is active.
        SceneCamera.IsEnabled = EditorState.PlayMode ? false : true;

        // Example.
        // Check for the 'C' key press to trigger cube spawning.
        if (Input.GetKey(Key.C, InputState.Down) && ViewportController.ViewportFocused)
        {
            // Log message indicating that 10 cubes have been spawned.
            Output.Log($"Spawned {CubesCount += 1000} Cubes");

            // Loop to spawn 1000 cubes with random attributes.
            for (int i = 0; i < 1000; i++)
            {
                // Create a new cube and add it to the Cubes entity.
                var newCube = SceneManager.MainScene.EntityManager.CreatePrimitive(PrimitiveTypes.Cube, Cubes, true);

                // Set the position of the new cube with an offset on the Y axis.
                newCube.Entity.Transform.LocalPosition = new(new Random().Next(-250, 250), new Random().Next(-250, 250), new Random().Next(-250, 250));
                // Set random rotation values for the new cube.
                newCube.Entity.Transform.EulerAngles = new(new Random().Next(1, 360), new Random().Next(1, 360), new Random().Next(1, 360));
                // Set random scale values for the new cube.
                newCube.Entity.Transform.LocalScale = new(new Random().Next(1, 3), new Random().Next(1, 3), new Random().Next(1, 3));
            }
        }
    }
}