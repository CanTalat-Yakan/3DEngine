using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Engine.Editor;
using Engine.Utilities;
using Editor.Controller;

namespace Engine
{
    internal class Core
    {
        public static Core Instance { get; private set; }

        public Input Input;
        public Time Time;
        public Scene Scene;
        public Renderer Renderer;
        public ImGui ImGui;

        public Core(SwapChainPanel swapChainPanel, TextBlock profile)
        {
            if (Instance is null)
                Instance = this;

            Renderer = new Renderer(swapChainPanel);
            Input = new Input();
            Time = new Time();
            Scene = new Scene();
            ImGui = new ImGui();

            OutputController.Log("Engine Initialized...");

            Scene.Awake();
            Scene.Start();

            CompositionTarget.Rendering += (s, e) =>
            {
                Renderer.Clear();

                Input.Update();

                Scene.Update();
                Scene.LateUpdate();

                Input.LateUpdate();

                Time.Update();

                Renderer.SetSolid();
                Scene.Render();
                Renderer.SetWireframe();
                Scene.Render();

                ImGui.Draw();

                Renderer.Present();

                profile.Text = Time.Profile;
                profile.Text += "\n\n" + Renderer.Profile;
                profile.Text += "\n\n" + Scene.Profile;
                profile.Text += "\n\n" + Scene.Camera.Transform.Position.ToString();
                profile.Text += "\n\n" + Scene.Camera.Transform.EulerAngles.ToString();
            };
        }
    }
}
