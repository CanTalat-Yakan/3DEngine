﻿global using System;
global using System.Numerics;

global using ImGuiNET;
global using Vortice.Dxc;

global using USD.NET;
global using pxr;

global using Engine.Buffers;
global using Engine.Components;
global using Engine.DataStructures;
global using Engine.ECS;
global using Engine.Editor;
global using Engine.Essentials;
global using Engine.Framework;
global using Engine.Graphics;
global using Engine.GUI;
global using Engine.Helpers;
global using Engine.Loaders;
global using Engine.Runtimes;
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
    public ComputeShaderCompiler ComputeShaderCompiler = new();

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
        Context.GraphicsContext.Initialize(Context.GraphicsDevice);
        Context.ComputeContext.Initialize(Context.GraphicsDevice);
        Context.UploadBuffer.Initialize(Context.GraphicsDevice, GraphicsDevice.GetMegabytesInByte(64));

        Context.LoadAssets();
        Context.LoadDefaultResources();

        if (Config.GUI)
        {
            GUIRenderer.Initialize();

            GUIInputHandler = new(hwnd);

            OnGUI += GUIRenderer.ProfileWindows;
        }

        Input.Initialize(hwnd);

        if (Config.Boot)
            SystemManager.MainEntityManager
                .CreateEntity(null, "Boot", "DefaultBoot", hide: true)
                .AddComponent<DefaultBoot>();

        //SceneLoader.Load(SystemManager, AssetPaths.SCENENES + "teapot.usdz");
        //SceneLoader.Save(AssetPaths.DIRECTORY + "test.usda", SystemManager);

        Compile();

        Output.Log("Engine Initialized...");
    }

    public void Frame()
    {
        Profiler.Start(out var stopwatch);

        if (!Context.IsRendering)
            return;

        OnInitialize?.Invoke();
        OnInitialize = null;

        Profiler.Benchmark(BeginRender);

        Time.Update();
        Input.Update();

        Profiler.Benchmark(SystemManager.ProcessSystems);

        if (EditorState.PlayModeStarted)
            Compile();

        if (Time.OnFixedFrame)
            Profiler.Benchmark(SystemManager.FixedUpdate);

        Profiler.Benchmark(SystemManager.Update);
        Profiler.Benchmark(SystemManager.LateUpdate);
        Profiler.Benchmark(SystemManager.Render);

        OnRender?.Invoke();

        if (Config.GUI)
            Profiler.Benchmark(RenderGUI);

        Profiler.Reset();

        Profiler.Benchmark(EndRender);

        Profiler.Stop(stopwatch, "Frame");
    }

    public void Dispose()
    {
        Context?.Dispose();

        OnDispose?.Invoke();

        GC.SuppressFinalize(this);
    }
}

public sealed partial class Kernel
{
    public void BeginRender()
    {
        Context.GraphicsContext.BeginCommand();

        Context.GPUUploadData();

        Context.GraphicsContext.BeginRender();
        Context.GraphicsContext.SetRenderTarget();
        Context.GraphicsContext.ClearRenderTarget();
        Context.GraphicsContext.ClearDepthStencil();
    }

    public void EndRender()
    {
        Context.GraphicsContext.EndRender();
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
        ScriptCompiler.Compile(EditorState.AssetsPath);
        ShaderCompiler.Compile(EditorState.AssetsPath);
        MaterialCompiler.Compile(EditorState.AssetsPath);
        ComputeShaderCompiler.Compile(EditorState.AssetsPath);

        SystemManager.ProcessSystems();

        SystemManager.Awake();
        SystemManager.Start();
    }
}