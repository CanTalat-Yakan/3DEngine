using System;

namespace Engine.Editor;

internal sealed class SceneBoot : EditorComponent, IHide
{
    public Camera SceneCamera;

    public Entity DefaultSky;
    public Entity Cubes;

    public int CubesCount;

    public override void OnAwake()
    {
        // Create a camera entity with the name "Camera".
        SceneCamera = SceneManager.MainScene.EntityManager.CreateCamera("Camera");
        SceneCamera.Entity.IsHidden = true;
        // Set the camera order to the maximum value.
        SceneCamera.CameraID = byte.MaxValue;

        // Add the ViewportController components to the camera entity.
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
        SceneManager.MainScene.EntityManager.CreateCamera("Camera", Tags.MainCamera.ToString());

        Cubes = SceneManager.MainScene.EntityManager.CreateEntity(null, "Cubes");
        SceneManager.MainScene.EntityManager.CreatePrimitive(PrimitiveTypes.Cube, parent: Cubes);

        Output.Log("Press 'C' to spawn 1000 Cubes");
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
                var newCube = SceneManager.MainScene.EntityManager.CreatePrimitive(PrimitiveTypes.Cube, parent: Cubes, hide: true).Entity;

                newCube.Transform.LocalPosition = new(rnd.Next(-250, 250), rnd.Next(-250, 250), rnd.Next(-250, 250));
                newCube.Transform.EulerAngles = new(rnd.Next(1, 360), rnd.Next(1, 360), rnd.Next(1, 360));
                newCube.Transform.LocalScale = new(rnd.Next(1, 3), rnd.Next(1, 3), rnd.Next(1, 3));

                newCube.AddComponent<HiddenCubeHoverEffect>();
            }
        }
    }
}

internal sealed class HiddenCubeHoverEffect : EditorComponent, IHide
{
    private float _verticalPostion;
    private float _factor = 3;
    private float _random;

    public override void OnStart()
    {
        _random = (float)new Random().NextDouble() * 10;
        _verticalPostion = Entity.Transform.LocalPosition.Y;
    } 

    public override void OnUpdate() =>
        Entity.Transform.LocalPosition.Y = MathF.Sin((float)Time.Timer + _random) * _factor + _verticalPostion;
}