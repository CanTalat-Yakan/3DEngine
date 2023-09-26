global using System.Numerics;
global using System;

global using Engine.Components;
global using Engine.Data;
global using Engine.ECS;
global using Engine.Editor;
global using Engine.Helper;
global using Engine.Utilities;

global using Key = Vortice.DirectInput.Key;

namespace Engine;

public sealed class Core
{
    public static Core Instance { get; private set; }

    public static string AssetsPath { get; private set; }
    public static bool PlayMode { get; private set; } = false;
    public static bool PlayModeStarted { get; private set; } = false;

    public event EventHandler OnRender;
    public event EventHandler OnInitialize;
    public event EventHandler OnDispose;

    public Renderer Renderer;
    public RuntimeCompiler RuntimeCompiler;
    public SceneManager SceneManager;

    public Core(Renderer renderer, nint hWnd, string assetsPath = null) =>
        Initialize(renderer, hWnd, assetsPath);

    public Core(Win32Window win32Window, string assetsPath = null) =>
        Initialize(new Renderer(win32Window), win32Window.Handle, assetsPath);

    public void Initialize(Renderer renderer, nint hWnd, string assetsPath = null)
    {
        // Initializes the singleton instance of the class, if it hasn't been already.
        if (Instance is null)
            Instance = this;

        AssetsPath = assetsPath;

        Input.Initialize(hWnd);

        Renderer = renderer;
        RuntimeCompiler = new();
        SceneManager = new();

        // Creates an entity with the "Boot" editor tag and adds a "SceneBoot" component to it.
        SceneManager.MainScene.EntityManager
            .CreateEntity(null, "Boot", EditorTags.SceneBoot.ToString())
            .AddComponent(new SceneBoot());

        // Compile all project scripts and add components for the editor's "AddComponent" function.
        RuntimeCompiler.CompileProjectScripts(AssetsPath);

        // Copies the List to the local array once to savely iterate to it.
        SceneManager.ProcessSystems();

        // Render Pipeline Init
        SceneManager.Awake();
        SceneManager.Start();

        Output.Log("Engine Initialized...");
    }

    public void Frame()
    {
        if (!Renderer.IsRendering)
            return;

        OnInitialize?.Invoke(null, null);
        OnInitialize = null;

        // Clears the render target, discarding the contents and preparing it for the next frame.
        Renderer.Clear();

        // Updates the time values, such as delta time and time scale,
        // used in the game or application.
        Time.Update();

        // Acquire and Poll Mouse and Keyboard and Update the States and the Input.
        Input.Fetch();
        Input.Update();

        // Copies the List to the local array once to savely iterate to it.
        SceneManager.ProcessSystems();

        // Invokes Awake and Start if play mode has started.
        if (PlayModeStarted)
        {
            // Gather Components for the Editor's AddComponent function.
            RuntimeCompiler.CompileProjectScripts();

            // Call Awake for all scenes again.
            SceneManager.Awake();
            // Call Start for all scenes again.
            SceneManager.Start();
        }

        // Call Update for all scenes.
        SceneManager.Update();
        // Call LateUpdate for all scenes.
        SceneManager.LateUpdate();

        if (Time.TimeStepEllapsed)
            FixedFrame();

        // Finishes the state of input processing.
        Input.LateUpdate();

        // Renders the scene twice, once in solid mode and once in wireframe mode.
        Renderer.Data.SetRasterizerDescFillMode();
        SceneManager.Render();
        Renderer.Data.SetRasterizerDescFillModeWireframe();
        SceneManager.Render();

        OnRender?.Invoke(null, null);

        // Presents the final rendered image on the screen.
        Renderer.Present();
    }

    public void FixedFrame()
    {
        // Call FixedUpdate for input.
        Input.FixedUpdate();
        // Call FixedUpdate for all scenes.
        SceneManager.FixedUpdate();
    }

    public void Dispose()
    {
        Renderer?.Dispose();
        Input.Dispose();
        OnDispose?.Invoke(null, null);
    }

    public void SetPlayMode(bool b) =>
        PlayMode = b;

    public void SetPlayModeStarted(bool b) =>
        PlayModeStarted = b;
}