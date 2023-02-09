using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System;
using Windows.Storage;
using Windows.System;
using System.Diagnostics;

namespace Editor.Controller
{
    internal enum EMessageType
    {
        Message,
        Warning,
        Error
    }

    internal struct SMessageInfo
    {
        public EMessageType Type;
        public string Script;
        public string Method;
        public int Line;
        public string Message;

        public string GetInfo() { return Script.Split("\\").Last() + $":{Line} ({Method})"; }
    }

    internal class Output
    {
        private static Dictionary<SMessageInfo, List<DateTime>> s_messageCollection = new();

        private static TextBlock s_status;
        private static Viewbox s_statusIcon;
        private static StackPanel s_stack;
        private static ScrollViewer s_scroll;
        private static AppBarToggleButton s_collapse;
        private static AppBarToggleButton s_filterMessages;
        private static AppBarToggleButton s_filterWarnings;
        private static AppBarToggleButton s_filterErrors;
        private static AppBarToggleButton s_pauseError;

        internal AppBarToggleButton _clearPlay;

        private static Dictionary<DateTime, SMessageInfo> _dic = new();

        public Output(StackPanel stack, ScrollViewer scroll, AppBarToggleButton collapse, AppBarToggleButton filterMessages, AppBarToggleButton filterWarnings, AppBarToggleButton filterErrors, AppBarToggleButton pauseError, AppBarToggleButton clearPlay)
        {
            s_stack = stack;
            s_scroll = scroll;
            s_collapse = collapse;
            s_filterMessages = filterMessages;
            s_filterWarnings = filterWarnings;
            s_filterErrors = filterErrors;
            s_pauseError = pauseError;
            _clearPlay = clearPlay;

            s_status = Main.Instance.Status;
            s_statusIcon = Main.Instance.StatusIcon;
        }

        public static void Log(object o, EMessageType t = EMessageType.Message, [CallerLineNumber] int l = 0, [CallerMemberName] string c = null, [CallerFilePath] string s = null) =>
        // Logs a message, with a string representation of an object.
        Log(o.ToString(), t, l, c, s);

        public static void Log(string m, EMessageType t = EMessageType.Message, [CallerLineNumber] int l = 0, [CallerMemberName] string c = null, [CallerFilePath] string s = null)
        {
            // Create a new instance of SMessageInfo with the given properties.
            SMessageInfo message = new()
            {
                Script = s,
                Method = c,
                Line = l,
                Message = m,
                Type = t
            };

            // Check if s_status is null and if it is, return,
            // to prevent setting the status if it isn't initialized.
            if (s_status is null)
                return;

            // Check if the "Pause on Error" checkbox is checked and if the message type is an error.
            if (s_pauseError.IsChecked.Value)
                if (t == EMessageType.Error)
                    // Pause the Playmode with the message type error.
                    Main.Instance.ControlPlayer.Pause();

            // Check if the message is not already in the message collection.
            if (!s_messageCollection.ContainsKey(message))
                // Add a new message to the collection.
                s_messageCollection.Add(message, new List<DateTime> { DateTime.Now });
            else
                // Find the message in the collection and add a new timestamp.
                s_messageCollection[message].Add(DateTime.Now);

            // Set the message to the Statusbar.
            SetStatus(message);

            // Iterate, sort, filter or collapse Messages and present them.
            IterateOutputMessages();
        }

        public static void IterateOutputMessages()
        {
            // Clear the s_stack children.
            s_stack.Children.Clear();

            // Initialize the message, warning and error counters.
            int numMessages = 0;
            int numWarnings = 0;
            int numErrors = 0;

            // Count the number of messages, warnings and errors in s_messageCollection.
            foreach (var kv in s_messageCollection)
            {
                switch (kv.Key.Type)
                {
                    case EMessageType.Message:
                        numMessages += kv.Value.Count;
                        break;
                    case EMessageType.Warning:
                        numWarnings += kv.Value.Count;
                        break;
                    case EMessageType.Error:
                        numErrors += kv.Value.Count;
                        break;
                    default:
                        break;
                }
            }

            // Update the label of the message, warning and error filters.
            s_filterMessages.Label = $"{numMessages} Messages";
            s_filterWarnings.Label = $"{numWarnings} Warnings";
            s_filterErrors.Label = $"{numErrors} Errors";

            // Clear a dictionary to store message info and timestamps.
            _dic.Clear();

            // Loop through the keys of the message collection.
            foreach (var k in s_messageCollection.Keys)
                // If collapse option is checked, add the last instance of each message to the dictionary.
                if (s_collapse.IsChecked.Value)
                    // Add the value and key to the dictionary.
                    _dic.Add(s_messageCollection[k].Last(), k);
                else
                    // Loop through the values of the message collection for each key.
                    foreach (var v in s_messageCollection[k])
                        // Add the value and key to the dictionary.
                        _dic.Add(v, k);

            // Order the dictionary by key.
            var l = _dic.OrderBy(key => key.Key);
            // Convert the ordered dictionary back to a dictionary.
            _dic = l.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);

            // Check the type of each message in the dictionary and add to s_stack if the corresponding filter is checked.
            foreach (var kv in _dic)
            {
                switch (kv.Value.Type)
                {
                    case EMessageType.Message:
                        // Check if the "Message" filter is checked.
                        if (!s_filterMessages.IsChecked.Value)
                            // If it's not, continue to the next iteration.
                            continue;
                        break;
                    case EMessageType.Warning:
                        // Check if the "Warning" filter is checked.
                        if (!s_filterWarnings.IsChecked.Value)
                            // If it's not, continue to the next iteration.
                            continue;
                        break;
                    case EMessageType.Error:
                        // Check if the "Error" filter is checked.
                        if (!s_filterErrors.IsChecked.Value)
                            // If it's not, continue to the next iteration.
                            continue;
                        break;
                    default:
                        break;
                }

                if (s_collapse.IsChecked.Value)
                    // Call the "CreateMessage" method and add the result to the children collection of the StackPanel
                    // with the count of messages send by the same message and line of code.
                    s_stack.Children.Add(CreateMessage(kv.Key, kv.Value, s_messageCollection[kv.Value].Count()));
                else
                    // Call the "CreateMessage" method and add the result to the children collection of the StackPanel.
                    s_stack.Children.Add(CreateMessage(kv.Key, kv.Value, null));
            }

            // Update the layout of the StackPanel.
            s_stack.UpdateLayout();
            // Scroll the ScrollViewer to the bottom to display the latest message.
            s_scroll.ChangeView(0, double.MaxValue, 1);
        }

        public void ClearOutput()
        {
            // Clear the message collection dictionary and the UI stack panel.
            s_messageCollection.Clear();
            s_stack.Children.Clear();

            // Reset the labels of the message, warning and error filters.
            s_filterMessages.Label = $" Messages";
            s_filterWarnings.Label = $" Warnings";
            s_filterErrors.Label = $" Errors";
        }

        private static void SetStatus(SMessageInfo m)
        {
            s_status.Text = m.Message.Split("\n")[0];

            if (m.Type == EMessageType.Warning)
                s_statusIcon.Child = new FontIcon() { Glyph = "\uE7BA" };
            else if (m.Type == EMessageType.Message)
                s_statusIcon.Child = new SymbolIcon() { Symbol = Symbol.Message };
            else if (m.Type == EMessageType.Error)
                s_statusIcon.Child = new SymbolIcon() { Symbol = Symbol.ReportHacked };
        }

        private static UIElement CreateMessage(DateTime d, SMessageInfo m, int? i)
        {
            // Content of the message.
            StackPanel stack = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top, Spacing = 10, Margin = new(10, 0, 0, 0), Padding = new(5) };
            Viewbox viewbox = new() { Width = 14, Height = 14 };
            if (m.Type == EMessageType.Warning)
                viewbox.Child = new FontIcon() { Glyph = "\uE7BA" };
            else if (m.Type == EMessageType.Message)
                viewbox.Child = new SymbolIcon() { Symbol = Symbol.Message };
            else if (m.Type == EMessageType.Error)
                viewbox.Child = new SymbolIcon() { Symbol = Symbol.ReportHacked };
            stack.Children.Add(viewbox);
            stack.Children.Add(new TextBlock() { Text = "[" + d.TimeOfDay.ToString("hh\\:mm\\:ss").ToString() + "]" });
            stack.Children.Add(new TextBlock() { Text = m.Message });

            // The flyout when clicked on the message.
            StackPanel stackFlyout = new() { Orientation = Orientation.Vertical };
            stackFlyout.Children.Add(new TextBlock() { Text = m.GetInfo() + "\n" });
            stackFlyout.Children.Add(new MarkdownTextBlock() { Text = m.Message, TextWrapping = TextWrapping.WrapWholeWords, Padding = new Thickness(2) });
            HyperlinkButton hyperlinkButton = new() { Content = Path.GetRelativePath(Directory.GetCurrentDirectory(), m.Script) + ":" + m.Line, Foreground = new SolidColorBrush(Colors.CadetBlue) };
            hyperlinkButton.Click += (s, e) => Process.Start(new ProcessStartInfo { FileName = Path.GetRelativePath(Directory.GetCurrentDirectory(), m.Script), UseShellExecute = true });
            stackFlyout.Children.Add(hyperlinkButton);
            Flyout flyout = new Flyout() { OverlayInputPassThroughElement = stack, Content = stackFlyout };

            // Create main grid that gets returned
            Grid grid = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
            grid.Children.Add(stack);

            // If there is a count the number gets shown on the right.
            if (i != null)
                grid.Children.Add(new TextBlock() { Margin = new(0, 0, 10, 0), Padding = new(5), MinWidth = 25, HorizontalAlignment = HorizontalAlignment.Right, Text = i.ToString() });

            // Set flyout to button that stretches along the grid.
            grid.Children.Add(new Button()
            {
                Flyout = flyout,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(
                    m.Type == EMessageType.Message ?
                        s_stack.Children.Count() % 2 == 0 ?
                            Colors.Transparent :
                            Windows.UI.Color.FromArgb(50, 10, 10, 10) :
                        m.Type == EMessageType.Error ?
                            Windows.UI.Color.FromArgb(88, 255, 0, 0) :
                            Windows.UI.Color.FromArgb(88, 255, 255, 0))
            });

            // return new message.
            return grid;
        }
    }
}
