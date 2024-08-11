global using System;
global using System.Numerics;

global using ImGuiNET;
global using Vortice.Dxc;

global using Engine.Buffer;
global using Engine.Components;
global using Engine.DataTypes;
global using Engine.ECS;
global using Engine.Editor;
global using Engine.Framework;
global using Engine.Graphics;
global using Engine.GUI;
global using Engine.Helper;
global using Engine.Loader;
global using Engine.Runtime;
global using Engine.Utilities;

namespace Engine;

public sealed partial class Kernel
{
    public static Kernel Instance { get; private set; }

    public event Action OnInitialize;
    public event Action OnRender;
    public event Action OnGUI;
    public event Action OnDispose;

    public CommonContext Context;

    public Config Config;

    public SystemManager SystemManager = new();

    public ScriptCompiler ScriptCompiler = new();
    public ShaderCompiler ShaderCompiler = new();
    public MaterialCompiler MaterialCompiler = new();

    public GUIRenderer GUIRenderer = new();
    public GUIInputHandler GUIInputHandler;
    public IntPtr GUIContext;

    public Kernel(Config config)
    {
        Config = config;

        Context = new CommonContext(this);
    }

    public void Initialize(IntPtr hwnd, Vortice.Mathematics.SizeI size, bool win32Window, string assetsPath = null)
    {
        Instance ??= this;

        EditorState.PlayMode = win32Window;
        EditorState.AssetsPath = assetsPath;

        Context.GraphicsDevice.Initialize(size, win32Window);
        Context.UploadBuffer.Initialize(Context.GraphicsDevice, GraphicsDevice.GetMegabytesInByte(64));
        Context.GraphicsContext.Initialize(Context.GraphicsDevice);

        Context.LoadAssets();
        Context.LoadDefaultResources();

        if (Config.GUI)
        {
            GUIRenderer.Initialize();

            GUIInputHandler = new(hwnd);

            OnGUI += GUIRenderer.ProfileWindows;
        }

        Input.Initialize(hwnd);

        SceneLoader.Load(out var systemManager, Paths.SCENENES + "teapot.usdz");

        var boot = SystemManager.MainEntityManager
            .CreateEntity(null, "Boot", hide: true)
            .AddComponent<SceneBoot>();

        //SceneLoader.Save(Paths.DIRECTORY + "test.usda", SystemManager);

        Compile();

        Output.Log("Engine Initialized...");
    }

    public void Frame()
    {
        Profiler.Start(out var stopwatch);
        Profiler.Start(out var stopwatch1);

        if (!Context.IsRendering)
            return;

        OnInitialize?.Invoke();
        OnInitialize = null;

        BeginRender();

        Profiler.Stop(stopwatch, "Begin Render");
        Profiler.Start(out stopwatch);

        Time.Update();
        Input.Update();

        SystemManager.ProcessSystems();

        if (EditorState.PlayModeStarted)
            Compile();

        Profiler.Stop(stopwatch, "Before Update");
        Profiler.Start(out stopwatch);

        Profiler.Start(out var stopwatch2);
        if (Time.OnFixedFrame)
            SystemManager.FixedUpdate();
        Profiler.Stop(stopwatch2, "Fixed Update");

        Profiler.Start(out var stopwatch3);
        SystemManager.Update();
        Profiler.Stop(stopwatch3, "Update");
        Profiler.Start(out var stopwatch4);
        SystemManager.LateUpdate();
        Profiler.Stop(stopwatch4, "Late Update");
        Profiler.Start(out var stopwatch5);
        SystemManager.Render();
        Profiler.Stop(stopwatch5, "Render Update");

        OnRender?.Invoke();

        Profiler.Stop(stopwatch, "After Update");

        if (Config.GUI)
            RenderGUI();

        Input.LateUpdate();

        Profiler.Reset();

        EndRender();

        Profiler.Stop(stopwatch1, "Frame");
    }

    public void Dispose()
    {
        Context?.Dispose();

        OnDispose?.Invoke();
    }
}

public sealed partial class Kernel
{
    public void BeginRender()
    {
        Context.GraphicsDevice.Begin();

        Context.GraphicsContext.BeginCommand();

        Context.GPUUploadData(Context.GraphicsContext);

        Context.GraphicsContext.SetDescriptorHeapDefault();
        Context.GraphicsContext.ScreenBeginRender();
        Context.GraphicsContext.SetRenderTargetScreen();
        Context.GraphicsContext.ClearRenderTargetScreen();
        Context.GraphicsContext.ClearDepthStencilScreen();
    }

    public void EndRender()
    {
        Context.GraphicsContext.ScreenEndRender();
        Context.GraphicsContext.EndCommand();
        Context.GraphicsContext.Execute();

        Context.GraphicsDevice.Present();
    }

    public void RenderGUI()
    {
        GUIRenderer.Update(GUIContext);
        GUIInputHandler.Update();

        SystemManager.GUI();

        OnGUI?.Invoke();

        GUIRenderer.Render();
    }

    public void Compile()
    {
        ScriptCompiler.CompileProjectScripts(EditorState.AssetsPath);
        ShaderCompiler.CompileProjectShaders(EditorState.AssetsPath);
        MaterialCompiler.CompileProjectMaterials(EditorState.AssetsPath);

        SystemManager.ProcessSystems();

        SystemManager.Awake();
        SystemManager.Start();
    }
}