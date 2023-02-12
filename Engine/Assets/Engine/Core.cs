using ImGuiNET;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Linq;
using System;
using Editor.Controller;
using Engine.ECS;
using Engine.Editor;
using Engine.Utilities;

namespace Engine
{
    internal class Core
    {
        public static Core Instance { get; private set; }

        public SceneManager SceneManager;
        public Renderer Renderer;
        public ComponentCollector ComponentCollector;
        public ImGuiRenderer ImGuiRenderer;

        private IntPtr _imGuiContext;

        private EPlayMode _playmode = EPlayMode.None;
        private TextBlock _profile;

        public Core(SwapChainPanel swapChainPanel, TextBlock profile)
        {
            // Initializes the singleton instance of the class, if it hasn't been already.
            if (Instance is null)
                Instance = this;

            // Assign local variable.
            _profile = profile;

            // Creates a new ImGui context and sets it as the current context.
            _imGuiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(_imGuiContext);

            // Initializes the renderer, scene manager, and ImGui renderer.
            Renderer = new(swapChainPanel);
            SceneManager = new();
            ComponentCollector = new();
            ImGuiRenderer = new();

            // Creates an entity with the "Boot" Editortag and adds a "SceneBoot" component to it.
            SceneManager.Scene.EntitytManager
                .CreateEntity(null, "Boot", EEditorTags.SceneBoot.ToString())
                .AddComponent(new SceneBoot());

            // Set the displaySize with the actual size of the SwapChainPanel.
            ImGui.GetIO().DisplaySize = new(
                (float)swapChainPanel.ActualWidth,
                (float)swapChainPanel.ActualHeight);

            // Gather Components for the Editor's AddComponent function.
            CollectComponents();

            Output.Log("Engine Initialized...");

            #region // Render Pipeline Loop
            // Call Awake method for all scenens.
            SceneManager.Awake();
            // Call Start method for all scenens.
            SceneManager.Start();

            // Adds an event handler for the CompositionTarget.Rendering event,
            // which is triggered when the composition system is rendering a frame.
            // The code inside the event handler will be executed each time the event is raised.
            CompositionTarget.Rendering += (s, e) => Frame();
            #endregion
        }

        public void Frame()
        {
            // Clears the render target, discarding the contents and preparing it for the next frame.
            Renderer.Clear();

            // Updates the input state, polling for any new events
            // or changes in the state of the pointer or the keyboard.
            Input.Update();

            // Invokes Awake and Start if playmode has started.
            if (CheckPlaymodeStarted())
            {
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

            // Updates and renders the ImGui user interface.
            UpdateImGui();
            ImGuiRenderer.Render(ImGui.GetDrawData());

            // Presents the final rendered image on the screen.
            Renderer.Present();

            // Updates the time values, such as delta time and time scale,
            // used in the game or application.
            Time.Update();

            // Updates the text of the profile with the profiling information.
            _profile.Text = Profiler.GetString();
        }

        public void CollectComponents()
        {
            // Collect all components in the Assembly
            // and ignore all components that have the IHide interface.
            var componentCollection = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => 
                    (typeof(Component).IsAssignableFrom(p) && !p.Equals(typeof(Component)))
                    && !(typeof(IHide).IsAssignableFrom(p) && !p.IsInterface))
                .ToArray();
            
            // Add components to the collector.
            ComponentCollector.AddComponents(componentCollection.ToArray());
        }

        public virtual void UpdateImGui()
        {
            ImGui.SetCurrentContext(_imGuiContext);
            var io = ImGui.GetIO();

            io.DeltaTime = (float)Time.Delta;

            ImGui.NewFrame();
            ImGui.Render();
        }

        private bool CheckPlaymodeStarted()
        {
            if (Main.Instance.PlayerControl.PlayMode == EPlayMode.Playing)
                if (_playmode != Main.Instance.PlayerControl.PlayMode)
                    return true;

            _playmode = Main.Instance.PlayerControl.PlayMode;
            return false;
        }
    }
}
