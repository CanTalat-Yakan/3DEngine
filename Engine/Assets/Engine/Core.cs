using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Engine.Editor;
using Engine.Utilities;
using Editor.Controller;
using System;
using ImGuiNET;
using System.Numerics;
using System.Diagnostics;
using System.Drawing;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine
{
    internal class Core
    {
        public static Core Instance { get; private set; }

        public Input Input;
        public Time Time;
        public SceneManager SceneManager;
        public Renderer Renderer;
        public ImGuiRenderer ImGuiRenderer;

        IntPtr imGuiContext;

        public Core(SwapChainPanel swapChainPanel, TextBlock profile)
        {
            if (Instance is null)
                Instance = this;

            imGuiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(imGuiContext);

            Renderer = new(swapChainPanel);
            Input = new();
            Time = new();
            SceneManager = new(new Scene());
            SceneManager.Scene.EntitytManager.CreateEntity(null, "Boot").AddComponent(new CustomScene());
            ImGuiRenderer = new();

            OutputController.Log("Engine Initialized...");

            ImGui.GetIO().DisplaySize = new Vector2((float)swapChainPanel.ActualWidth, (float)swapChainPanel.ActualHeight);

            SceneManager.Awake();
            SceneManager.Start();

            CompositionTarget.Rendering += (s, e) =>
            {
                Renderer.Clear();

                Input.Update();

                SceneManager.Update();
                SceneManager.LateUpdate();

                Input.LateUpdate();

                Time.Update();

                Renderer.SetSolid();
                SceneManager.Render();
                Renderer.SetWireframe();
                SceneManager.Render();

                UpdateImGui();
                ImGuiRenderer.Render(ImGui.GetDrawData());

                Renderer.Present();

                profile.Text = Time.Profile;
                profile.Text += "\n\n" + Renderer.Profile;
                profile.Text += "\n\n" + SceneManager.Profile();
                //profile.Text += "\n\n" + SceneManager.Camera.Transform.Position.ToString();
                //profile.Text += "\n\n" + SceneManager.Camera.Transform.EulerAngles.ToString();
            };
        }

        public virtual void UpdateImGui()
        {
            ImGui.SetCurrentContext(imGuiContext);
            var io = ImGui.GetIO();

            io.DeltaTime = (float)Time.s_Delta;

            ImGui.NewFrame();
            ImGui.Render();
        }
    }
}
