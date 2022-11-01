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
        internal Input m_Input;
        internal Time m_Time;
        internal Scene m_Scene;
        internal Renderer m_Render;
        internal ImGui m_Gui;

        internal Loop(SwapChainPanel _swapChainPanel, TextBlock _textBlock)
        {
            m_Render = new Renderer(_swapChainPanel);
            m_Input = new Input();
            m_Time = new Time();
            m_Scene = new Scene();
            m_Gui = new ImGui();

            m_Scene.Awake();
            m_Scene.Start();
            
            CompositionTarget.Rendering += (s, e) =>
            {
                m_Render.Clear();

                m_Input.Update();

                m_Scene.Update();
                m_Scene.LateUpdate();

                m_Input.LateUpdate();

                m_Time.Update();

                m_Render.SetSolid();
                m_Scene.Render();
                m_Render.SetWireframe();
                m_Scene.Render();

                m_Gui.Draw();

                m_Render.Present();

                _textBlock.Text = m_Time.m_Profile;
                _textBlock.Text += "\n\n" + m_Render.m_Profile;
                _textBlock.Text += "\n\n" + m_Scene.m_Profile;
                _textBlock.Text += "\n\n" + m_Scene.m_Camera.m_Transform.m_Position.ToString();
                _textBlock.Text += "\n\n" + m_Scene.m_Camera.m_Transform.m_EulerAngles.ToString();
            };
        }
    }
}
