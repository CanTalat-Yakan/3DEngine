global using Editor.Controller;
global using Engine.Components;
global using Engine.Data;
global using Engine.ECS;
global using Engine.Editor;
global using Engine.Helper;
global using Engine.Utilities;
global using System.Numerics;
global using System;

using ImGuiNET;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Engine;

internal class Core
{
    public static Core Instance { get; private set; }

    public SceneManager SceneManager;
    public Renderer Renderer;
    public RuntimeCompiler RuntimeCompiler;

    public ImGuiRenderer ImGuiRenderer;
    private IntPtr _imGuiContext;

    public Core(SwapChainPanel swapChainPanel)
    {
        // Initializes the singleton instance of the class, if it hasn't been already.
        if (Instance is null)
            Instance = this;

        // Initializes the renderer, scene manager, and the runtimeCompiler.
        Renderer = new(swapChainPanel);
        SceneManager = new();
        RuntimeCompiler = new();

        // Creates an entity with the "Boot" editortag and adds a "SceneBoot" component to it.
        SceneManager.Scene.EntityManager
            .CreateEntity(null, "Boot", EditorTags.SceneBoot.ToString())
            .AddComponent(new SceneBoot());

        // Compile all projec scripts and add components for the editor's "AddComponent" function.
        RuntimeCompiler.CompileProjectScripts();

        #region // ImGui
        // Creates a new ImGui context and sets it as the current context.
        _imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imGuiContext);

        // Initializes the ImGui renderer.
        ImGuiRenderer = new();

        // Set the displaySize with the actual size of the SwapChainPanel.
        ImGui.GetIO().DisplaySize = new(
            (float)swapChainPanel.ActualWidth,
            (float)swapChainPanel.ActualHeight);
        #endregion

        Output.Log("Engine Initialized...");

        // Render Pipeline Loop
        SceneManager.Awake();
        SceneManager.Start();
    }

    public void Frame()
    {
        if (!Renderer.IsRendering)
            return;

        // Clears the render target, discarding the contents and preparing it for the next frame.
        Renderer.Clear();

        // Updates the input state, polling for any new events
        // or changes in the state of the pointer or the keyboard.
        Input.Update();

        // Invokes Awake and Start if playmode has started.
        if (Main.Instance.PlayerControl.CheckPlaymodeStarted())
        {
            // Gather Components for the Editor's AddComponent function.
            RuntimeCompiler.CompileProjectScripts();

            // Call Awake method for all scenens again.
            SceneManager.Awake();
            // Call Start method for all scenens again.
            SceneManager.Start();
        }

        // Call Update method for all scenens.
        SceneManager.Update();
        // Call LateUpdate method for all scenens.
        SceneManager.LateUpdate();

        // Finishes the state of input processing.
        Input.LateUpdate();

        // Renders the scene twice, once in solid mode and once in wireframe mode.
        Renderer.SetRasterizerDesc();
        SceneManager.Render();
        Renderer.SetRasterizerDesc(false);
        SceneManager.Render();

#if EDITOR
        // Updates and renders the ImGui user interface.
        ImGuiRenderer.Update(_imGuiContext);
        ImGuiRenderer.Render(ImGui.GetDrawData());

        // Presents the final rendered image on the screen.
        Renderer.Present();
#endif

        // Updates the time values, such as delta time and time scale,
        // used in the game or application.
        Time.Update();
    }
}
