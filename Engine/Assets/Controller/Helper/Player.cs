﻿using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Editor.Controller
{
    internal enum EPlayMode
    {
        None,
        Playing,
        Paused,
    }

    internal class Player
    {
        public EPlayMode PlayMode;

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
            // Check if the current playmode is None.
            if (PlayMode == EPlayMode.None)
                // If the clear play option is checked, clear the output.
                if (_output._clearPlay.IsChecked.Value)
                    _output.ClearOutput();

            // Change the border brush, thickness, padding, and margin of the viewport control.
            Main.Instance.LayoutControl.ViewPort._viewPortControl.Content.BorderBrush = new SolidColorBrush(Colors.GreenYellow);
            Main.Instance.LayoutControl.ViewPort._viewPortControl.Content.BorderThickness = new(_play.IsChecked.Value ? 2 : 0);
            Main.Instance.LayoutControl.ViewPort._viewPortControl.Content.Padding = new(_play.IsChecked.Value ? -2 : 0);
            Main.Instance.LayoutControl.ViewPort._viewPortControl.Content.Margin = new(_play.IsChecked.Value ? 2 : 0);

            // Set the status of the app bar buttons.
            SetStatusAppBarButtons(_play.IsChecked.Value);

            // Log the current status of the game.
            Output.Log(_play.IsChecked.Value ? "Now Playing..." : "Stopped Gamemode");
        }

        public void Pause()
        {
            // Set enum variable Playmode value based on the checked state of "_pause".
            PlayMode = _pause.IsChecked.Value ? EPlayMode.Paused : EPlayMode.Playing;
            // Enable the "_forward" Button with the checked state of "_pause".
            _forward.IsEnabled = _pause.IsChecked.Value;

            // Change the border brush to either Orange or GreenYellow depending on the checked state of "_pause".
            Main.Instance.LayoutControl.ViewPort._viewPortControl.Content.BorderBrush = new SolidColorBrush(_pause.IsChecked.Value ? Colors.Orange : Colors.GreenYellow);

            // Log the current status of the game.
            Output.Log(_pause.IsChecked.Value ? "Paused Gamemode" : "Continued Gamemode");
        }

        public void Forward()
        {
            // Check if the current playmode is not paused.
            if (PlayMode != EPlayMode.Paused)
                return;

            // Advance the game by one frame.
            Engine.Core.Instance.Frame();

            // Log the current status of the game.
            Output.Log("Stepped Forward");
        }

        public void Kill()
        {
            // Uncheck the play button.
            _play.IsChecked = false;

            // Disable all AppBarButtons in the Statusbar.
            SetStatusAppBarButtons(false);

            // Log the current status of the game.
            Output.Log("Killed Process of GameInstance!");
        }

        private void SetStatusAppBarButtons(bool b)
        {
            // Update the play mode of the game instance.
            PlayMode = b ? EPlayMode.Playing : EPlayMode.None;

            // Enable/disable the pause button based on the play mode.
            _pause.IsEnabled = b;
            // Uncheck the pause button if the game is not in playing mode.
            _pause.IsChecked = false;
            // Enable/disable the forward button based on the play mode.
            if (!b) _forward.IsEnabled = b;

            // Update the label and icon of the play button based on the play mode.
            _play.Label = b ? "Stop" : "Play";
            _play.Icon = b ? new SymbolIcon(Symbol.Stop) : new SymbolIcon(Symbol.Play);
        }
    }
}
