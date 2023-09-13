﻿global using System.Numerics;
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
    public static bool PlayMode { get; private set; }
    public static bool PlayModeStarted { get; private set; }

    public SceneManager SceneManager;
    public Renderer Renderer;
    public RuntimeCompiler RuntimeCompiler;

    public Core(Renderer renderer, nint hWnd, string assetsPath = null)
    {
        // Initializes the singleton instance of the class, if it hasn't been already.
        if (Instance is null)
            Instance = this;

        AssetsPath = assetsPath;

        Input.Initialize(hWnd);

        // Initializes the renderer, scene manager, and the runtimeCompiler.
        Renderer = renderer;

        Initialize();
    }

    public Core(Win32Window win32Window, string assetsPath = null)
    {
        // Initializes the singleton instance of the class, if it hasn't been already.
        if (Instance is null)
            Instance = this;

        AssetsPath = assetsPath;

        Input.Initialize(win32Window.Handle);

        // Initializes the renderer, scene manager, and the runtimeCompiler.
        Renderer = new(win32Window);

        Initialize();
    }

    public void Initialize()
    {
        RuntimeCompiler = new();

        SceneManager = new();

        // Creates an entity with the "Boot" editor tag and adds a "SceneBoot" component to it.
        SceneManager.MainScene.EntityManager
            .CreateEntity(null, "Boot", EditorTags.SceneBoot.ToString())
            .AddComponent(new SceneBoot());

        // Compile all project scripts and add components for the editor's "AddComponent" function.
        RuntimeCompiler.CompileProjectScripts(AssetsPath);

        Output.Log("Engine Initialized...");

        // Render Pipeline Loop
        SceneManager.Awake();
        SceneManager.Start();
    }

    public void Dispose()
    {
        Renderer?.Dispose();
        Input.Dispose();
    }

    public void Frame()
    {
        if (!Renderer.IsRendering)
            return;

        // Clears the render target, discarding the contents and preparing it for the next frame.
        Renderer.Clear();

        // Acquire and Poll Mouse and Keyboard and Update the States and the Input.
        Input.Fetch();
        Input.Update();

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

        // Finishes the state of input processing.
        Input.LateUpdate();

        // Renders the scene twice, once in solid mode and once in wireframe mode.
        Renderer.SetRasterizerDesc();
        SceneManager.Render();
        Renderer.SetRasterizerDesc(Vortice.Direct3D11.FillMode.Wireframe);
        SceneManager.Render();

        // Presents the final rendered image on the screen.
        Renderer.Present();

        // Updates the time values, such as delta time and time scale,
        // used in the game or application.
        Time.Update();
    }

    public void SetPlayMode(bool b) =>
        PlayMode = b;

    public void SetPlayModeStarted(bool b) =>
        PlayModeStarted = b;
}