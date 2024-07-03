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
global using Engine.Runtime;
global using Engine.Utilities;
using Engine.Loader;

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

        //SceneLoader.Load(out var systemManager, Paths.DIRECTORY + "Kitchen_set.usd");

        var boot = SystemManager.MainScene
            .CreateEntity(null, "Boot", hide: true)
            .AddComponent<SceneBoot>();

        SceneLoader.Save(Paths.DIRECTORY + "test.usda", SystemManager);

        Compile();

        Output.Log("Engine Initialized...");
    }

    public void Frame()
    {
        if (!Context.IsRendering)
            return;

        OnInitialize?.Invoke();
        OnInitialize = null;

        BeginRender();

        Time.Update();
        Input.Update();

        SystemManager.ProcessSystems();

        if (EditorState.PlayModeStarted)
            Compile();

        if (Time.OnFixedFrame)
            SystemManager.FixedUpdate();

        SystemManager.Update();
        SystemManager.LateUpdate();
        SystemManager.Render();

        OnRender?.Invoke();

        if (Config.GUI)
            RenderGUI();

        Input.LateUpdate();

        Profiler.Reset();

        EndRender();
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