using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Editor.Controls
{
    public enum EPlayMode
    {
        NONE,
        PLAYING,
        PAUSED
    }

    public class PlayerController
    {
        public EPlayMode PlayMode;

        AppBarToggleButton _play;
        AppBarToggleButton _pause;
        AppBarButton _forward;
        TextBlock _status;
        OutputController _output;

        public PlayerController(AppBarToggleButton play, AppBarToggleButton pause, AppBarButton forward)
        {
            _play = play;
            _pause = pause;
            _forward = forward;
            _output = MainController.Instance.LayoutControl.Output.OutputControl;
            _status = MainController.Instance.Status;
        }

        void SetStatusAppBarButtons(bool b)
        {
            PlayMode = b ? EPlayMode.PLAYING : EPlayMode.NONE;

            _pause.IsEnabled = b;
            _pause.IsChecked = false;
            if(!b)
                _forward.IsEnabled = b;

            _play.Label = b ? "Stop" : "Play";
            _play.Icon = b ? new SymbolIcon(Symbol.Stop) : new SymbolIcon(Symbol.Play);
        }

        void SetStatus(string _s)
        {
            _status.Text = _s;
        }

        public void Play()
        {
            if (PlayMode == EPlayMode.NONE)
                if (_output._clearPlay.IsChecked.Value)
                    _output.ClearOutput();

            MainController.Instance.LayoutControl.ViewPort.BorderColor.BorderBrush = new SolidColorBrush(Colors.GreenYellow);
            MainController.Instance.LayoutControl.ViewPort.BorderColor.BorderThickness = new Thickness(_play.IsChecked.Value ? 2 : 0);

            SetStatusAppBarButtons(_play.IsChecked.Value);

            SetStatus(_play.IsChecked.Value ? "Now Playing..." : "Stopped Gamemode");
        }

        public void Pause()
        {
            PlayMode = _pause.IsChecked.Value ? EPlayMode.PAUSED : EPlayMode.PLAYING;

            _forward.IsEnabled = _pause.IsChecked.Value;
            MainController.Instance.LayoutControl.ViewPort.BorderColor.BorderBrush = new SolidColorBrush(_pause.IsChecked.Value ? Colors.Orange : Colors.GreenYellow);

            SetStatus(_pause.IsChecked.Value ? "Paused Gamemode" : "Continued Gamemode");
        }

        public void Forward()
        {
            if (PlayMode != EPlayMode.PAUSED)
                return;

            OutputController.Log("Stepped Forward..");

            SetStatus("Stepped Forward");
        }

        public void Kill()
        {
            _play.IsChecked = false;

            SetStatusAppBarButtons(false);

            SetStatus("Killed Process of GameInstance!");
        }
    }
}
