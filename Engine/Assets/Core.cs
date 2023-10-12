global using System.Numerics;
global using System;

global using Engine.Components;
global using Engine.Data;
global using Engine.ECS;
global using Engine.Gui;
global using Engine.Editor;
global using Engine.Helper;
global using Engine.Rendering;
global using Engine.Runtime;
global using Engine.SceneSystem;
global using Engine.Utilities;

namespace Engine;

public sealed class Core
{
    public static Core Instance { get; private set; }

    public event EventHandler OnRender;
    public event EventHandler OnInitialize;
    public event EventHandler OnGui;
    public event EventHandler OnDispose;

    public Renderer Renderer;
    public SceneManager SceneManager;

    public ScriptCompiler ScriptCompiler;
    public ShaderCompiler ShaderCompiler;
    public MaterialCompiler MaterialCompiler;

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
        ScriptCompiler = new();
        ShaderCompiler = new();
        MaterialCompiler = new();
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

        // Compile all project scripts and add the components to the collection for the AddComponent function.
        ScriptCompiler.CompileProjectScripts(EditorState.AssetsPath);
        // Compile all project shaders and add them to the collection.
        ShaderCompiler.CompileProjectShaders(EditorState.AssetsPath);
        // Compile all project materials and add them to the collection.
        MaterialCompiler.CompileProjectMaterials(EditorState.AssetsPath);

        // Copies the List to the local array once to safely iterate to it.
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

        // Invoke the OnInitialize Event.
        try { OnInitialize?.Invoke(null, null); }
        catch (Exception ex) { Output.Log(ex.Message, MessageType.Error); }
        finally { OnInitialize = null; }

        // Clears the render target and preparing it for the next frame
        Renderer.Clear();
        // Set the viewport size.
        Renderer.Data.SetViewport(Renderer.Size);

        // Updates the time values, such as delta time and time scale,
        // used in the game or application.
        Time.Update();

        // Acquire and Poll Mouse and Keyboard and Update the States and the Input.
        Input.Fetch();
        Input.Update();

        // Copies the List to the local array once to safely iterate to it.
        SceneManager.ProcessSystems();

        // Invokes Awake and Start if play mode has started.
        if (EditorState.PlayModeStarted)
        {
            // Gather Materials.
            MaterialCompiler.CompileProjectMaterials(EditorState.AssetsPath);
            // Gather Components.
            ScriptCompiler.CompileProjectScripts(EditorState.AssetsPath);

            // Call Awake for all scenes again.
            SceneManager.Awake();
            // Call Start for all scenes again.
            SceneManager.Start();
        }

        // Call the FixedFrame when the timeStep elapsed.
        if (Time.TimeStepElapsed)
            // Call FixedUpdate for all scenes.
            SceneManager.FixedUpdate();

        // Call Update for all scenes.
        SceneManager.Update();
        // Call LateUpdate for all scenes.
        SceneManager.LateUpdate();

        // Finishes the state of input processing.
        Input.LateUpdate();

        // Render the Scenes in the given RenderMode.
        Render(Renderer.Config.RenderMode);
        
        // Invoke the OnRender Event.
        OnRender?.Invoke(null, null);

        #region // ImGui
        // Update the ImGuiRenderer.
        _imGuiRenderer.Update(_imGuiContext, Renderer.Size);
        // Update the ImGuiInputHandler.
        _imGuiInputHandler.Update(Renderer.Config.SuperSample);

        // Invoke the OnGui Event.
        OnGui?.Invoke(null, null);

        // Call the Render Gui for all scenes.
        SceneManager.Gui();

        // Render the ImGui.
        ImGuiNET.ImGui.Render();
        // Render the DrawaData from ImGui with the ImGuiRenderer.
        _imGuiRenderer.Render();
        #endregion

        // Presents the final rendered image on the screen.
        Renderer.Present();
    }

    public void Dispose()
    {
        Renderer?.Dispose();
        SceneManager?.Dispose();
        Input.Dispose();
        OnDispose?.Invoke(null, null);
    }

    public void Render(RenderMode renderMode)
    {
        switch (renderMode)
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