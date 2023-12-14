global using System.Numerics;
global using System;

global using ImGuiNET;

global using Engine.Components;
global using Engine.Data;
global using Engine.ECS;
global using Engine.Editor;
global using Engine.GUI;
global using Engine.Helper;
global using Engine.Rendering;
global using Engine.Runtime;
global using Engine.SceneSystem;
global using Engine.Utilities;

namespace Engine;

public sealed class Core
{
    public static Core Instance { get; private set; }

    public event Action OnRender;
    public event Action OnInitialize;
    public event Action OnGUI;
    public event Action OnDispose;

    public Renderer Renderer;
    public SceneManager SceneManager;

    public ScriptCompiler ScriptCompiler;
    public ShaderCompiler ShaderCompiler;
    public MaterialCompiler MaterialCompiler;

    public GUIRenderer GUIRenderer;
    public GUIInputHandler GUIInputHandler;
    private IntPtr _imguiContext;

    public Core(Renderer renderer, nint hwnd, string assetsPath = null) =>
        Initialize(renderer, hwnd, assetsPath);

    public void Initialize(Renderer renderer, nint hwnd, string assetsPath = null)
    {
        // Set the singleton instance of the class, if it hasn't been already.
        Instance ??= this;

        EditorState.AssetsPath = assetsPath;

        Input.Initialize(hwnd);

        Renderer = renderer;
        ScriptCompiler = new();
        ShaderCompiler = new();
        MaterialCompiler = new();
        SceneManager = new();

        if (Renderer.Config.GUI)
        {
            _imguiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(_imguiContext);

            GUIRenderer = new();
            GUIInputHandler = new(hwnd);
        }

        // Creates an entity with the Boot editor tag and adds a SceneBoot component to it.
        var boot = SceneManager.MainScene.EntityManager
            .CreateEntity(null, "Boot")
            .AddComponent<SceneBoot>();
        boot.Entity.IsHidden = true;

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
        OnInitialize?.Invoke();
        OnInitialize = null;

        // Clears the render target, preparing it for the next frame.
        Renderer.BeginFrame();
        // Set the viewport size.
        Renderer.Data.SetViewport(Renderer.Size);

        // Clear the StringBuilder for additional profiling.
        Profiler.Reset();

        // Updates time values, such as delta time and time scale.
        Time.Update();

        // Acquire and Poll Mouse and Keyboard and Update the States and the Input.
        Input.Fetch();
        Input.Update();

        // Copies the List to the local array once for safe iteration.
        SceneManager.ProcessSystems();

        // Invokes Awake and Start if play mode has started.
        OnEditorPlayMode();

        // Call FixedUpdate for all scenes when the timeStep has elapsed.
        if (Time.OnFixedFrame)
            SceneManager.FixedUpdate();

        // Call Update, LateUpdate, and Render for all scenes.
        SceneManager.Update();
        SceneManager.LateUpdate();
        SceneManager.Render();

        // Invoke the OnRender Event.
        OnRender?.Invoke();

        // Render the GUI with ImGui if enabled in the configuration.
        if (Renderer.Config.GUI)
            RenderGUI();

        // Finish the state of input processing.
        Input.LateUpdate();

        // End the frame and prepare for the next.
        Renderer.EndFrame();
        // Copy the final rendered image into the back buffer.
        Renderer.Resolve();

        //Renderer.Execute();

        // Present the back buffer on the screen.
        Renderer.Present();
        // Wait for the GPU to finish processing commands.
        Renderer.WaitIdle();
    }

    public void RenderGUI()
    {
        // Update the ImGuiRenderer.
        GUIRenderer.Update(_imguiContext, Renderer.Size);
        // Update the ImGuiInputHandler.
        GUIInputHandler.Update();

        // Call the Render the GUI for all scenes.
        SceneManager.GUI();

        // Invoke the OnGUI Event.
        OnGUI?.Invoke();

        // Render the ImGui.
        ImGui.Render();
        // Render the DrawaData from ImGui with the ImGuiRenderer.
        GUIRenderer.Render();
    }

    public void OnEditorPlayMode()
    {
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
    }

    public void Dispose()
    {
        Renderer?.Dispose();
        SceneManager?.Dispose();

        Input.Dispose();

        OnDispose?.Invoke();
    }
}