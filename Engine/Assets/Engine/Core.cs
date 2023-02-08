using ImGuiNET;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Editor.Controller;
using Engine.Editor;
using Engine.Utilities;
using Engine.ECS;
using Vortice.Direct3D11;

namespace Engine
{
    internal class Core
    {
        public static Core Instance { get; private set; }

        public SceneManager SceneManager;
        public Renderer Renderer;
        public ImGuiRenderer ImGuiRenderer;

        private IntPtr _imGuiContext;

        private EPlaymode _playmode = EPlaymode.None;

        public Core(SwapChainPanel swapChainPanel, TextBlock profile)
        {
            // Initializes the singleton instance of the class, if it hasn't been already.
            if (Instance is null)
                Instance = this;

            // Creates a new ImGui context and sets it as the current context.
            _imGuiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(_imGuiContext);

            // Initializes the renderer, scene manager, and ImGui renderer.
            Renderer = new(swapChainPanel);
            SceneManager = new();
            ImGuiRenderer = new();

            // Creates an entity with the "Boot" Editortag and adds a "SceneBoot" component to it.
            SceneManager.Scene.EntitytManager.CreateEntity(null, "Boot", EEditorTags.SceneBoot.ToString()).AddComponent(new SceneBoot());
            ImGui.GetIO().DisplaySize = new((float)swapChainPanel.ActualWidth, (float)swapChainPanel.ActualHeight);

            Output.Log("Engine Initialized...");


            // Invokes Awake
            SceneManager.Awake();
            // Invokes Start
            SceneManager.Start();

            #region // Render Pipeline Loop
            // Adds an event handler for the CompositionTarget.Rendering event,
            // which is triggered when the composition system is rendering a frame.
            // The code inside the event handler will be executed each time the event is raised.
            CompositionTarget.Rendering += (s, e) =>
            {
                // Clears the render target, discarding the contents and preparing it for the next frame.
                Renderer.Clear();

                // Updates the input state, polling for any new events or changes in the state of the pointer or the keyboard.
                Input.Update();

                // Invokes Awake and Start if playmode has started.
                if (CheckIfPlaymodeStarted())
                {
                    SceneManager.Awake();
                    SceneManager.Start();
                }

                // Invokes Update
                SceneManager.Update();
                // Invokes LateUpdate
                SceneManager.LateUpdate();

                // Finishes the state of input processing.
                Input.LateUpdate();

                // Renders the scene twice, once in solid mode and once in wireframe mode.
                Renderer.SetRasterizerDesc();
                SceneManager.Render();
                Renderer.SetRasterizerDesc(FillMode.Wireframe);
                SceneManager.Render();

                // Updates and renders the ImGui user interface.
                UpdateImGui();
                ImGuiRenderer.Render(ImGui.GetDrawData());

                // Presents the final rendered image on the screen.
                Renderer.Present();

                // Updates the time values, such as delta time and time scale, used in the game or application.
                Time.Update();

                // Updates the text of the profile with the profiling information.
                profile.Text = Profiler.ToString();
            };
            #endregion
        }

        public virtual void UpdateImGui()
        {
            ImGui.SetCurrentContext(_imGuiContext);
            var io = ImGui.GetIO();

            io.DeltaTime = (float)Time.Delta;

            ImGui.NewFrame();
            ImGui.Render();
        }

        private bool CheckIfPlaymodeStarted()
        {
            if (Main.Instance.ControlPlayer.Playmode == EPlaymode.Playing)
                if (_playmode != Main.Instance.ControlPlayer.Playmode)
                    return true;

            _playmode = Main.Instance.ControlPlayer.Playmode;
            return false;
        }
    }
}
