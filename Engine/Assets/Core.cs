global using Engine.Components;
global using Engine.Data;
global using Engine.ECS;
global using Engine.Editor;
global using Engine.Helper;
global using Engine.Utilities;
global using System.Numerics;
global using System;

#if EDITOR
using Engine.Editor;
#endif

namespace Engine;

public sealed class Core
{
    public static Core Instance { get; private set; }

    public SceneManager SceneManager;
    public Renderer Renderer;
    public RuntimeCompiler RuntimeCompiler;

    //public Editor.ImGuiRenderer ImGuiRenderer;
    //private IntPtr _imGuiContext;

    public Core(Renderer renderer = null, Win32Window win32Window = null)
    {
        // Initializes the singleton instance of the class, if it hasn't been already.
        if (Instance is null)
            Instance = this;

        // Initializes the renderer, scene manager, and the runtimeCompiler.
        Renderer = renderer is not null ? renderer : new(win32Window);
        RuntimeCompiler = new();

        SceneManager = new();

        // Creates an entity with the "Boot" editortag and adds a "SceneBoot" component to it.
        SceneManager.MainScene.EntityManager
            .CreateEntity(null, "Boot", EditorTags.SceneBoot.ToString())
            .AddComponent(new SceneBoot());

        // Compile all projec scripts and add components for the editor's "AddComponent" function.
        RuntimeCompiler.CompileProjectScripts();

        //#region // ImGui
        //// Creates a new ImGui context and sets it as the current context.
        //_imGuiContext = ImGui.CreateContext();
        //ImGui.SetCurrentContext(_imGuiContext);

        //// Initializes the ImGui renderer.
        //ImGuiRenderer = new();

        //// Set the displaySize with the actual size of the SwapChainPanel.
        //ImGui.GetIO().DisplaySize = new(
        //    Renderer.Size.Width,
        //    Renderer.Size.Height);
        //#endregion

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

#if EDITOR
        // Updates and renders the ImGui user interface.
        ImGuiRenderer.Update(_imGuiContext);
        ImGuiRenderer.Render(ImGui.GetDrawData());

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
#endif

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

        // Presents the final rendered image on the screen.
        Renderer.Present();

        // Updates the time values, such as delta time and time scale,
        // used in the game or application.
        Time.Update();
    }
}