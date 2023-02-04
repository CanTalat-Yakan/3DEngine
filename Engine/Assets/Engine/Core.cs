using ImGuiNET;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Editor.Controller;
using Engine.Editor;
using Engine.Utilities;
using Engine.ECS;

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
            if (Instance is null)
                Instance = this;

            _imGuiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(_imGuiContext);

            Renderer = new(swapChainPanel);
            SceneManager = new();
            ImGuiRenderer = new();

            SceneManager.Scene.EntitytManager.CreateEntity(null, "Boot", EEditorTags.SceneBoot.ToString()).AddComponent(new SceneBoot());
            ImGui.GetIO().DisplaySize = new((float)swapChainPanel.ActualWidth, (float)swapChainPanel.ActualHeight);

            Output.Log("Engine Initialized...");

            SceneManager.Awake();
            SceneManager.Start();

            CompositionTarget.Rendering += (s, e) =>
            {
                Renderer.Clear();

                Input.Update();

                if (CheckIfPlaymodeStarted())
                {
                    SceneManager.Awake();
                    SceneManager.Start();
                }

                SceneManager.Update();
                SceneManager.LateUpdate();

                Input.LateUpdate();

                Renderer.SetSolid();
                SceneManager.Render();
                Renderer.SetWireframe();
                SceneManager.Render();

                UpdateImGui();
                ImGuiRenderer.Render(ImGui.GetDrawData());

                Renderer.Present();

                Time.Update();

                profile.Text = Profiler.ToString();
            };
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
