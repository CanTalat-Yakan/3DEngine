﻿namespace Engine.Editor;

internal sealed class SceneBoot : EditorComponent, IHide
{
    public Camera SceneCamera;

    public Entity DefaultSky;
    public Entity Cubes;

    public Entity ExampleCamera;

    public int CubesCount;
    public int EntityCount;

    public override void OnAwake()
    {
        // Create a camera entity with the name "Camera".
        SceneCamera = Entity.Manager.CreateCamera("Camera");
        SceneCamera.Entity.Data.IsHidden = true;
        // Set the camera order to the maximum value.
        SceneCamera.CameraID = byte.MaxValue;

        // Add the ViewportController components to the camera entity.
        SceneCamera.Entity.AddComponent<ViewportController>().SetCamera(SceneCamera);

        // Set the initial position and rotation of the camera entity.
        SceneCamera.Entity.Transform.LocalPosition = new(3, 4, 5);
        SceneCamera.Entity.Transform.EulerAngles = new(35, -150, 0);

        // Create a sky entity in the scene.
        var defaultSky = Entity.Manager
            .CreateEntity()
            .AddComponent<DefaultSky>();
        defaultSky.Initialize();

        DefaultSky = defaultSky.Entity;
    }

    public override void OnStart()
    {
        ExampleCamera = Entity.Manager.CreateCamera("Camera", Tags.MainCamera.ToString()).Entity;
        ExampleCamera.Transform.LocalPosition = new(3, 4, 5);
        ExampleCamera.Transform.EulerAngles = new(35, -150, 0);

        Cubes = Entity.Manager.CreateEntity(null, "Cubes");
        Entity.Manager.CreatePrimitive(PrimitiveTypes.Cube, parent: Cubes);

        Output.Log("Press 'C' to spawn 1000 Cubes");
        Output.Log("Press 'E' to spawn 1000 Entities");
        Output.Log("Press 'V' to add a Viewport Controller");
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
                var newCube = Entity.Manager.CreatePrimitive(PrimitiveTypes.Cube, parent: Cubes, hide: true).Entity;

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
                Entity.Manager.CreateEntity(hide: true);
        }

        if (Input.GetKey(Key.V, InputState.Down) && ViewportController.ViewportFocused)
        {
            Output.Log($"Viewport Controller added");

            if (!ExampleCamera.HasComponent<ViewportController>())
                ExampleCamera.AddComponent<ViewportController>();
        }
    }
}

internal sealed class HoverEffect : Component, IHide
{
    private Vector3 _localPosition;
    private float _verticalPosition;
    private float _factor = 3;
    private float _random;

    public override void OnStart()
    {
        _random = (float)new Random().NextDouble() * 10;
        _verticalPosition = Entity.Transform.LocalPosition.Y;
    }

    public override void OnUpdate() =>
        Entity.Transform.SetPosition(y: MathF.Sin((float)Time.Timer + _random) * _factor + _verticalPosition);
}