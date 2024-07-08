namespace Engine.Editor;

internal sealed class SceneBoot : EditorComponent, IHide
{
    public Camera SceneCamera;

    public EntityData DefaultSky;
    public EntityData Cubes;

    public int CubesCount;
    public int EntityCount;

    public override void OnAwake()
    {
        // Create a camera entity with the name "Camera".
        SceneCamera = SystemManager.MainScene.CreateCamera("Camera");
        SceneCamera.EntityData.IsHidden = true;
        // Set the camera order to the maximum value.
        SceneCamera.CameraID = byte.MaxValue;

        // Add the ViewportController components to the camera entity.
        SceneCamera.EntityData.AddComponent<ViewportController>().SetCamera(SceneCamera);

        // Set the initial position and rotation of the camera entity.
        SceneCamera.EntityData.Transform.LocalPosition = new(3, 4, 5);
        SceneCamera.EntityData.Transform.EulerAngles = new(35, -150, 0);

        // Create a sky entity in the scene.
        var defaultSky = SystemManager.MainScene
            .CreateEntity()
            .AddComponent<DefaultSky>();
        defaultSky.Initialize();

        DefaultSky = defaultSky.EntityData;
    }

    public override void OnStart()
    {
        var exampleCamera = SystemManager.MainScene.CreateCamera("Camera", Tags.MainCamera.ToString()).EntityData;
        exampleCamera.Transform.LocalPosition = new(3, 4, 5);
        exampleCamera.Transform.EulerAngles = new(35, -150, 0);

        Cubes = SystemManager.MainScene.CreateEntity(null, "Cubes");
        SystemManager.MainScene.CreatePrimitive(PrimitiveTypes.Cube, parent: Cubes);

        Output.Log("Press 'C' to spawn 1000 Cubes");
        Output.Log("Press 'E' to spawn 1000 Entities");
    }

    public override void OnUpdate()
    {
        // Deactivate the SceneCamera when the play mode is active.
        SceneCamera.IsEnabled = !EditorState.PlayMode;

        if (Input.GetKey(Key.C, InputState.Pressed) && ViewportController.ViewportFocused)
        {
            Output.Log($"Spawned {CubesCount += 1000} Cubes");

            Random rnd = new();

            for (int i = 0; i < 1000; i++)
            {
                var newCube = SystemManager.MainScene.CreatePrimitive(PrimitiveTypes.Cube, parent: Cubes, hide: true).EntityData;

                newCube.Transform.LocalPosition = new(rnd.Next(-250, 250), rnd.Next(-250, 250), rnd.Next(-250, 250));
                newCube.Transform.EulerAngles = new(rnd.Next(1, 360), rnd.Next(1, 360), rnd.Next(1, 360));
                newCube.Transform.LocalScale = new(rnd.Next(1, 3), rnd.Next(1, 3), rnd.Next(1, 3));

                newCube.AddComponent<HoverEffect>();
            }
        }

        if (Input.GetKey(Key.E, InputState.Pressed) && ViewportController.ViewportFocused)
        {
            Output.Log($"Spawned {EntityCount += 1000} Entities");


            for (int i = 0; i < 1000; i++)
                SystemManager.MainScene.CreateEntity(hide: true).AddComponent<HoverEffect>();
        }
    }
}

internal sealed class HoverEffect : Component, IHide
{
    private float _verticalPosition;
    private float _factor = 3;
    private float _random;

    public override void OnStart()
    {
        _random = (float)new Random().NextDouble() * 10;
        _verticalPosition = EntityData.Transform.LocalPosition.Y;
    }

    public override void OnUpdate() =>
        EntityData.Transform.LocalPosition.Y = MathF.Sin((float)Time.Timer + _random) * _factor + _verticalPosition;
}