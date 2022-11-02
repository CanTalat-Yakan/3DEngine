using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Engine.Editor;
using Engine.Utilities;

namespace Engine
{
    public class Core
    {
        public static Core Instance { get; private set; }

        public Input Input;
        public Time Time;
        public Scene Scene;
        public Renderer Renderer;
        public ImGui ImGui;

        public Core(SwapChainPanel swapChainPanel, TextBlock textBlock)
        {
            #region //Create Instance
            Instance = this;
            #endregion

            Renderer = new Renderer(swapChainPanel);
            Input = new Input();
            Time = new Time();
            Scene = new Scene();
            ImGui = new ImGui();

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

                textBlock.Text = Time.Profile;
                textBlock.Text += "\n\n" + Renderer.Profile;
                textBlock.Text += "\n\n" + Scene.Profile;
                textBlock.Text += "\n\n" + Scene.Camera.Transform.Position.ToString();
                textBlock.Text += "\n\n" + Scene.Camera.Transform.EulerAngles.ToString();
            };
        }
    }
}
