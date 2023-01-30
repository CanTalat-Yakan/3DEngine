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

        public static void Log(string m, EMessageType t = EMessageType.Message, [CallerLineNumber] int l = 0, [CallerMemberName] string c = null, [CallerFilePath] string s = null)
        {
            SMessageInfo message = new() { Script = s, Method = c, Line = l, Message = m, Type = t };

            if (s_pauseError.IsChecked.Value)
                if (t == EMessageType.Error)
                    Main.Instance.ControlPlayer.Pause();

            if (!s_messageCollection.ContainsKey(message))
                s_messageCollection.Add(message, new() { DateTime.Now });
            else
                s_messageCollection[message].Add(DateTime.Now);

            SetStatus(message);

            IterateOutputMessages();
        }

        public static void IterateOutputMessages()
        {
            Dictionary<DateTime, SMessageInfo> dic = new();
            s_stack.Children.Clear();

            int numMessages = 0;
            int numWarnings = 0;
            int numErrors = 0;
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
            s_filterMessages.Label = $"{numMessages} Messages";
            s_filterWarnings.Label = $"{numWarnings} Warnings";
            s_filterErrors.Label = $"{numErrors} Errors";

            if (s_collapse.IsChecked.Value)
            {
                foreach (var k in s_messageCollection.Keys)
                    dic.Add(s_messageCollection[k].Last(), k);

                var l = dic.OrderBy(key => key.Key);
                dic = l.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);

                foreach (var kv in dic)
                {
                    switch (kv.Value.Type)
                    {
                        case EMessageType.Message:
                            if (!s_filterMessages.IsChecked.Value)
                                continue;
                            break;
                        case EMessageType.Warning:
                            if (!s_filterWarnings.IsChecked.Value)
                                continue;
                            break;
                        case EMessageType.Error:
                            if (!s_filterErrors.IsChecked.Value)
                                continue;
                            break;
                        default:
                            break;
                    }
                    s_stack.Children.Add(CreateMessage(kv.Key, kv.Value, s_messageCollection[kv.Value].Count()));
                }
            }
            else
            {
                foreach (var k in s_messageCollection.Keys)
                    foreach (var v in s_messageCollection[k])
                        dic.Add(v, k);

                var l = dic.OrderBy(key => key.Key);
                dic = l.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);

                foreach (var kv in dic)
                {
                    switch (kv.Value.Type)
                    {
                        case EMessageType.Message:
                            if (!s_filterMessages.IsChecked.Value)
                                continue;
                            break;
                        case EMessageType.Warning:
                            if (!s_filterWarnings.IsChecked.Value)
                                continue;
                            break;
                        case EMessageType.Error:
                            if (!s_filterErrors.IsChecked.Value)
                                continue;
                            break;
                        default:
                            break;
                    }
                    s_stack.Children.Add(CreateMessage(kv.Key, kv.Value, null));
                }
            }

            s_stack.UpdateLayout();
            s_scroll.ChangeView(0, double.MaxValue, 1);
        }

        public void ClearOutput()
        {
            s_messageCollection.Clear();
            s_stack.Children.Clear();

            s_filterMessages.Label = $"  Messages";
            s_filterWarnings.Label = $"  Warnings";
            s_filterErrors.Label = $"  Errors";
        }

        private static void SetStatus(SMessageInfo m)
        {
            s_status.Text = m.Message;

            if (m.Type == EMessageType.Warning)
                s_statusIcon.Child = new FontIcon() { Glyph = "\uE7BA" };
            else if (m.Type == EMessageType.Message)
                s_statusIcon.Child = new SymbolIcon() { Symbol = Symbol.Message };
            else if (m.Type == EMessageType.Error)
                s_statusIcon.Child = new SymbolIcon() { Symbol = Symbol.ReportHacked };
        }

        private async void OpenMessage(string path)
        {
            var file = await ApplicationData.Current.LocalFolder.GetFileAsync(path);

            if (file != null)
                await Launcher.LaunchFileAsync(file);
        }

        private static UIElement CreateMessage(DateTime d, SMessageInfo m, int? i)
        {
            //Content of the message
            StackPanel stack = new() { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new(10, 0, 0, 0), Padding = new(5) };
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

            //The flyout when clicked on the message
            StackPanel stackFlyout = new() { Orientation = Orientation.Vertical };
            stackFlyout.Children.Add(new TextBlock() { Text = m.GetInfo() + "\n" });
            stackFlyout.Children.Add(new MarkdownTextBlock() { Text = m.Message, Padding = new Thickness(2) });
            HyperlinkButton hyperlinkButton = new() { Content = Path.GetRelativePath(Directory.GetCurrentDirectory(), m.Script) + ":" + m.Line, Foreground = new SolidColorBrush(Colors.CadetBlue) };
            hyperlinkButton.Click += (s, e) => Process.Start(new ProcessStartInfo { FileName = Path.GetRelativePath(Directory.GetCurrentDirectory(), m.Script), UseShellExecute = true });
            stackFlyout.Children.Add(hyperlinkButton);
            Flyout flyout = new Flyout() { OverlayInputPassThroughElement = stack, Content = stackFlyout };

            //Create main grid that gets returned
            Grid grid = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
            grid.Children.Add(stack);
            //If there is a count the number gets shown on the right
            if (i != null)
                grid.Children.Add(new TextBlock() { Margin = new(0, 0, 10, 0), Padding = new(5), MinWidth = 25, HorizontalAlignment = HorizontalAlignment.Right, Text = i.ToString() });
            //Set flyout to button that stretches along the grid
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

            return grid;
        }
    }
}
