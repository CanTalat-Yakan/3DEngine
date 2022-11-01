using CommunityToolkit.WinUI.UI.Helpers;
using Editor.UserControls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Core;
using Engine.Editor;
using Engine.Utilities;

namespace Engine
{
    internal class Loop
    {
        internal Input input;
        internal Time time;
        internal Scene scene;
        internal Renderer render;
        internal ImGui gui;

        internal Loop(SwapChainPanel _swapChainPanel, TextBlock _textBlock)
        {
            render = new Renderer(_swapChainPanel);
            input = new Input();
            time = new Time();
            scene = new Scene();
            gui = new ImGui();

            scene.Awake();
            scene.Start();
            
            CompositionTarget.Rendering += (s, e) =>
            {
                render.Clear();

                input.Update();

                scene.Update();
                scene.LateUpdate();

                input.LateUpdate();

                time.Update();

                render.SetSolid();
                scene.Render();
                render.SetWireframe();
                scene.Render();

                gui.Draw();

                render.Present();

                _textBlock.Text = time.profile;
                _textBlock.Text += "\n\n" + render.profile;
                _textBlock.Text += "\n\n" + scene.profile;
                _textBlock.Text += "\n\n" + scene.camera.transform.position.ToString();
                _textBlock.Text += "\n\n" + scene.camera.transform.eulerAngles.ToString();
            };
        }
    }
}
