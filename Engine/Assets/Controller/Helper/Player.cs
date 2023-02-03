using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Editor.Controller
{
    internal enum EPlaymode
    {
        None,
        Playing,
        Paused,
    }

    internal class Player
    {
        public EPlaymode Playmode;

        private AppBarToggleButton _play;
        private AppBarToggleButton _pause;
        private AppBarButton _forward;
        private TextBlock _status;
        private Output _output;

        public Player(AppBarToggleButton play, AppBarToggleButton pause, AppBarButton forward)
        {
            _play = play;
            _pause = pause;
            _forward = forward;
            _output = Main.Instance.LayoutControl.Output._outputControl;
            _status = Main.Instance.Status;
        }

        public void Play()
        {
            if (Playmode == EPlaymode.None)
                if (_output._clearPlay.IsChecked.Value)
                    _output.ClearOutput();

            Main.Instance.LayoutControl.ViewPort._viewPortControl.GridMain.BorderBrush = new SolidColorBrush(Colors.GreenYellow);
            Main.Instance.LayoutControl.ViewPort._viewPortControl.GridMain.BorderThickness = new(_play.IsChecked.Value ? 2 : 0);
            Main.Instance.LayoutControl.ViewPort._viewPortControl.GridMain.Padding = new(_play.IsChecked.Value ? -2 : 0);
            Main.Instance.LayoutControl.ViewPort._viewPortControl.GridMain.Margin = new(_play.IsChecked.Value ? 2 : 0);

            SetStatusAppBarButtons(_play.IsChecked.Value);

            Output.Log(_play.IsChecked.Value ? "Now Playing..." : "Stopped Gamemode");
        }

        public void Pause()
        {
            Playmode = _pause.IsChecked.Value ? EPlaymode.Paused : EPlaymode.Playing;

            _forward.IsEnabled = _pause.IsChecked.Value;
            Main.Instance.LayoutControl.ViewPort._viewPortControl.GridMain.BorderBrush = new SolidColorBrush(_pause.IsChecked.Value ? Colors.Orange : Colors.GreenYellow);

            Output.Log(_pause.IsChecked.Value ? "Paused Gamemode" : "Continued Gamemode");
        }

        public void Forward()
        {
            if (Playmode != EPlaymode.Paused)
                return;

            Output.Log("Stepped Forward");
        }

        public void Kill()
        {
            _play.IsChecked = false;

            SetStatusAppBarButtons(false);

            Output.Log("Killed Process of GameInstance!");
        }

        private void SetStatusAppBarButtons(bool b)
        {
            Playmode = b ? EPlaymode.Playing : EPlaymode.None;

            _pause.IsEnabled = b;
            _pause.IsChecked = false;
            if (!b)
                _forward.IsEnabled = b;

            _play.Label = b ? "Stop" : "Play";
            _play.Icon = b ? new SymbolIcon(Symbol.Stop) : new SymbolIcon(Symbol.Play);
        }
    }
}
