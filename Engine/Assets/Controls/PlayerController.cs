using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Editor.Controls
{
    internal enum EPlayMode
    {
        NONE,
        PLAYING,
        PAUSED
    }
    internal class PlayerController
    {
        internal EPlayMode m_PlayMode;

        AppBarToggleButton m_play;
        AppBarToggleButton m_pause;
        AppBarButton m_forward;
        TextBlock m_status;
        OutputController m_output;

        internal PlayerController(AppBarToggleButton play, AppBarToggleButton pause, AppBarButton forward)
        {
            m_play = play;
            m_pause = pause;
            m_forward = forward;
            m_output = MainController.Singleton.m_Layout.m_Output.m_Control;
            m_status = MainController.Singleton.m_Status;
        }

        void SetStatusAppBarButtons(bool _b)
        {
            m_PlayMode = _b ? EPlayMode.PLAYING : EPlayMode.NONE;

            m_pause.IsEnabled = _b;
            m_pause.IsChecked = false;
            if(!_b)
                m_forward.IsEnabled = _b;

            m_play.Label = _b ? "Stop" : "Play";
            m_play.Icon = _b ? new SymbolIcon(Symbol.Stop) : new SymbolIcon(Symbol.Play);
        }
        void SetStatus(string _s)
        {
            m_status.Text = _s;
        }

        internal void Play()
        {
            if (m_PlayMode == EPlayMode.NONE)
                if (m_output.m_ClearPlay.IsChecked.Value)
                    m_output.ClearOutput();

            MainController.Singleton.m_Layout.m_ViewPort.m_BorderBrush.BorderBrush = new SolidColorBrush(Colors.GreenYellow);
            MainController.Singleton.m_Layout.m_ViewPort.m_BorderBrush.BorderThickness = new Thickness(m_play.IsChecked.Value ? 2 : 0);

            SetStatusAppBarButtons(m_play.IsChecked.Value);

            SetStatus(m_play.IsChecked.Value ? "Now Playing..." : "Stopped Gamemode");
        }
        internal void Pause()
        {
            m_PlayMode = m_pause.IsChecked.Value ? EPlayMode.PAUSED : EPlayMode.PLAYING;

            m_forward.IsEnabled = m_pause.IsChecked.Value;
            MainController.Singleton.m_Layout.m_ViewPort.m_BorderBrush.BorderBrush = new SolidColorBrush(m_pause.IsChecked.Value ? Colors.Orange : Colors.GreenYellow);

            SetStatus(m_pause.IsChecked.Value ? "Paused Gamemode" : "Continued Gamemode");
        }
        internal void Forward()
        {
            if (m_PlayMode != EPlayMode.PAUSED)
                return;

            OutputController.Log("Stepped Forward..");

            SetStatus("Stepped Forward");
        }
        internal void Kill()
        {
            m_play.IsChecked = false;

            SetStatusAppBarButtons(false);

            SetStatus("Killed Process of GameInstance!");
        }
    }
}
