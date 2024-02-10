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

        ScriptCompiler.CompileProjectScripts(EditorState.AssetsPath);
        ShaderCompiler.CompileProjectShaders(EditorState.AssetsPath);
        MaterialCompiler.CompileProjectMaterials(EditorState.AssetsPath);

        SceneManager.ProcessSystems();

        // Render Pipeline Initialization
        SceneManager.Awake();
        SceneManager.Start();

        Output.Log("Engine Initialized...");
    }

    public void Frame()
    {
        if (!Renderer.IsRendering)
            return;

        OnInitialize?.Invoke();
        OnInitialize = null;

        Renderer.BeginFrame();
        Renderer.Data.SetViewport(Renderer.Size);

        Profiler.Reset();

        Time.Update();

        Input.Fetch();
        Input.Update();

        SceneManager.ProcessSystems();

        OnEditorPlayMode();

        if (Time.OnFixedFrame)
            SceneManager.FixedUpdate();

        SceneManager.Update();
        SceneManager.LateUpdate();
        SceneManager.Render();

        OnRender?.Invoke();

        if (Renderer.Config.GUI)
            RenderGUI();

        Input.LateUpdate();

        Renderer.EndFrame();
        Renderer.Resolve();

        Renderer.Present();
        Renderer.WaitIdle();
    }

    public void RenderGUI()
    {
        GUIRenderer.Update(_imguiContext, Renderer.Size);
        GUIInputHandler.Update();

        SceneManager.GUI();

        OnGUI?.Invoke();

        ImGui.Render();
        GUIRenderer.Render();
    }

    public void OnEditorPlayMode()
    {
        if (EditorState.PlayModeStarted)
        {
            ScriptCompiler.CompileProjectScripts(EditorState.AssetsPath);
            ShaderCompiler.CompileProjectShaders(EditorState.AssetsPath);
            MaterialCompiler.CompileProjectMaterials(EditorState.AssetsPath);

            SceneManager.Awake();
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