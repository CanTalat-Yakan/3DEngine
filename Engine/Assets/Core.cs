global using System.Numerics;
global using System;

global using Engine.Components;
global using Engine.Data;
global using Engine.ECS;
global using Engine.Editor;
global using Engine.Helper;
global using Engine.Utilities;

namespace Engine;

public sealed class Core
{
    public static Core Instance { get; private set; }

    public event EventHandler OnRender;
    public event EventHandler OnInitialize;
    public event EventHandler OnDispose;

    public Renderer Renderer;
    public MaterialCompiler MaterialCompiler;
    public RuntimeCompiler RuntimeCompiler;
    public SceneManager SceneManager;

    private ImGuiRenderer _imGuiRenderer;
    private ImGuiInputHandler _imGuiInputHandler;
    private IntPtr _imGuiContext;

    public Core(Renderer renderer, nint hwnd, string assetsPath = null) =>
        Initialize(renderer, hwnd, assetsPath);

    public Core(Win32Window win32Window, string assetsPath = null) =>
        Initialize(new Renderer(win32Window), win32Window.Handle, assetsPath);

    public void Initialize(Renderer renderer, nint hwnd, string assetsPath = null)
    {
        // Initializes the singleton instance of the class, if it hasn't been already.
        if (Instance is null)
            Instance = this;

        EditorState.AssetsPath = assetsPath;

        Input.Initialize(hwnd);

        Renderer = renderer;
        RuntimeCompiler = new();
        SceneManager = new();

        #region // ImGui
        _imGuiContext = ImGuiNET.ImGui.CreateContext();
        ImGuiNET.ImGui.SetCurrentContext(_imGuiContext);

        _imGuiRenderer = new();
        _imGuiInputHandler = new(hwnd);
        #endregion

        // Creates an entity with the Boot editor tag and adds a SceneBoot component to it.
        SceneManager.MainScene.EntityManager
            .CreateEntity(null, "Boot", EditorTags.SceneBoot.ToString())
            .AddComponent(new SceneBoot());

        // Compile all project materials and add them to the collection.
        MaterialCompiler.CompileProjectMaterials(EditorState.AssetsPath);
        // Compile all project scripts and add the components to the collection for the AddComponent function.
        RuntimeCompiler.CompileProjectScripts(EditorState.AssetsPath);

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

        try { OnInitialize?.Invoke(null, null); }
        catch (Exception ex) { throw new Exception(ex.Message); }
        finally { OnInitialize = null; }

        // Clears the render target,
        // discarding the contents and preparing it for the next frame
        // and sets the viewport size.
        Renderer.Clear();
        Renderer.Data.SetViewport(Renderer.Size);

        // Updates the time values, such as delta time and time scale,
        // used in the game or application.
        Time.Update();

        // Acquire and Poll Mouse and Keyboard and Update the States and the Input.
        Input.Fetch();
        Input.Update();

        // Copies the List to the local array once to savely iterate to it.
        SceneManager.ProcessSystems();

        // Invokes Awake and Start if play mode has started.
        if (EditorState.PlayModeStarted)
        {
            // Gather Materials.
            MaterialCompiler.CompileProjectMaterials(EditorState.AssetsPath);
            // Gather Components.
            RuntimeCompiler.CompileProjectScripts(EditorState.AssetsPath);

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

        SetFillMode(Renderer.Config.RenderMode);

        #region // ImGui
        _imGuiRenderer.Update(_imGuiContext, Renderer.Size);
        _imGuiInputHandler.Update(Renderer.Config.SuperSample);

        //ImGui.ShowDemoWindow();

        ImGuiNET.ImGui.Render();
        _imGuiRenderer.Render();
        #endregion

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
        SceneManager?.Dispose();
        Input.Dispose();
        OnDispose?.Invoke(null, null);
    }

    public void SetFillMode(RenderMode renderMode)
    {
        switch (Renderer.Config.RenderMode)
        {
            case RenderMode.Shaded:
                Renderer.Data.SetRasterizerDescFillMode();
                SceneManager.Render();
                break;
            case RenderMode.Wireframe:
                Renderer.Data.SetRasterizerDescFillModeWireframe();
                SceneManager.Render();
                break;
            case RenderMode.ShadedWireframe:
                // Renders the scene twice,
                // once in solid mode and once in wireframe mode.
                Renderer.Data.SetRasterizerDescFillMode();
                SceneManager.Render();
                Renderer.Data.SetRasterizerDescFillModeWireframe();
                SceneManager.Render();
                break;
        }
    }
}