using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Storage;
using Windows.System;

namespace Engine.Assets.Controls
{
    internal enum EMessageType
    {
        MESSAGE,
        WARNING,
        ERROR
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
    internal class COutput
    {
        static Dictionary<SMessageInfo, List<DateTime>> m_messageCollection = new Dictionary<SMessageInfo, List<DateTime>>();

        static TextBlock m_status;
        static StackPanel m_stack;
        static ScrollViewer m_scroll;
        static AppBarToggleButton m_collapse;
        static AppBarToggleButton m_filterMessages;
        static AppBarToggleButton m_filterWarnings;
        static AppBarToggleButton m_filterErrors;
        static AppBarToggleButton m_pauseError;
        internal AppBarToggleButton m_ClearPlay;

        internal COutput(StackPanel _stack, ScrollViewer _scroll, AppBarToggleButton _collapse, AppBarToggleButton _filterMessages, AppBarToggleButton _filterWarnings, AppBarToggleButton _filterErrors, AppBarToggleButton _pauseError, AppBarToggleButton _clearPlay)
        {
            m_stack = _stack;
            m_scroll = _scroll;
            m_collapse = _collapse;
            m_filterMessages = _filterMessages;
            m_filterWarnings = _filterWarnings;
            m_filterErrors = _filterErrors;
            m_pauseError = _pauseError;
            m_ClearPlay = _clearPlay;

            m_status = CMain.Singleton.m_Status;
        }

        static void SetStatus(SMessageInfo _m)
        {
            m_status.Text = _m.Message;
        }

        async void OpenMessage(string _path)
        {
            var file = await ApplicationData.Current.LocalFolder.GetFileAsync(_path);

            if (file != null)
                await Launcher.LaunchFileAsync(file);
        }

        static UIElement CreateMessage(DateTime _d, SMessageInfo _m, int? _i)
        {
            //Content of the message
            StackPanel stack = new StackPanel() { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(10, 0, 0, 0) };
            if (_m.Type == EMessageType.WARNING)
                stack.Children.Add(new FontIcon() { Glyph = "\uE7BA" });
            else
                stack.Children.Add(new SymbolIcon() { Symbol = _m.Type == EMessageType.MESSAGE ? Symbol.Message : Symbol.ReportHacked });
            stack.Children.Add(new TextBlock() { Text = "[" + _d.TimeOfDay.ToString("hh\\:mm\\:ss").ToString() + "]" });
            stack.Children.Add(new TextBlock() { Text = _m.Message });

            //The flyout when clicked on the message
            StackPanel stackFlyout = new StackPanel() { Orientation = Orientation.Vertical };
            stackFlyout.Children.Add(new TextBlock() { Text = _m.GetInfo() + "\n" });
            stackFlyout.Children.Add(new MarkdownTextBlock() { Text = _m.Message, Padding = new Thickness(2) });
            stackFlyout.Children.Add(new HyperlinkButton() { Content = Path.GetRelativePath(Directory.GetCurrentDirectory(), _m.Script) + ":" + _m.Line, Foreground = new SolidColorBrush(Colors.CadetBlue) });
            Flyout flyout = new Flyout() { OverlayInputPassThroughElement = stack, Content = stackFlyout };

            //Create main grid that gets returned
            Grid grid = new Grid() { HorizontalAlignment = HorizontalAlignment.Stretch };
            grid.Children.Add(stack);
            //If there is a count the number gets shown on the right
            if (_i != null)
                grid.Children.Add(new TextBlock() { Margin = new Thickness(0, 0, 10, 0), MinWidth = 25, HorizontalAlignment = HorizontalAlignment.Right, Text = _i.ToString() });
            //Set flyout to button that stretches along the grid
            grid.Children.Add(new Button()
            {
                Flyout = flyout,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(
                    _m.Type == EMessageType.MESSAGE ?
                        m_stack.Children.Count() % 2 == 0 ?
                            Colors.Transparent :
                            Windows.UI.Color.FromArgb(50, 10, 10, 10) :
                        _m.Type == EMessageType.ERROR ?
                            Windows.UI.Color.FromArgb(88, 255, 0, 0) :
                            Windows.UI.Color.FromArgb(88, 255, 255, 0))
            });

            return grid;
        }

        public static void Log(string _m, EMessageType _t = EMessageType.MESSAGE, [CallerLineNumber] int _l = 0, [CallerMemberName] string _c = null, [CallerFilePath] string _s = null)
        {
            SMessageInfo message = new SMessageInfo() { Script = _s, Method = _c, Line = _l, Message = _m, Type = _t };

            if (m_pauseError.IsChecked.Value)
                if (_t == EMessageType.ERROR)
                    CMain.Singleton.m_Player.Pause();

            if (!m_messageCollection.ContainsKey(message))
                m_messageCollection.Add(message, new List<DateTime>() { DateTime.Now });
            else
                m_messageCollection[message].Add(DateTime.Now);

            SetStatus(message);

            IterateOutputMessages();
        }

        internal static void IterateOutputMessages()
        {
            Dictionary<DateTime, SMessageInfo> dic = new Dictionary<DateTime, SMessageInfo>();
            m_stack.Children.Clear();

            int numMessages = 0;
            int numWarnings = 0;
            int numErrors = 0;
            foreach (var kv in m_messageCollection)
            {
                switch (kv.Key.Type)
                {
                    case EMessageType.MESSAGE:
                        numMessages += kv.Value.Count;
                        break;
                    case EMessageType.WARNING:
                        numWarnings += kv.Value.Count;
                        break;
                    case EMessageType.ERROR:
                        numErrors += kv.Value.Count;
                        break;
                    default:
                        break;
                }
            }
            m_filterMessages.Label = $"{numMessages} Messages";
            m_filterWarnings.Label = $"{numWarnings} Warnings";
            m_filterErrors.Label = $"{numErrors} Errors";

            if (m_collapse.IsChecked.Value)
            {
                foreach (var k in m_messageCollection.Keys)
                    dic.Add(m_messageCollection[k].Last(), k);

                var l = dic.OrderBy(key => key.Key);
                dic = l.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);

                foreach (var kv in dic)
                {
                    switch (kv.Value.Type)
                    {
                        case EMessageType.MESSAGE:
                            if (!m_filterMessages.IsChecked.Value)
                                continue;
                            break;
                        case EMessageType.WARNING:
                            if (!m_filterWarnings.IsChecked.Value)
                                continue;
                            break;
                        case EMessageType.ERROR:
                            if (!m_filterErrors.IsChecked.Value)
                                continue;
                            break;
                        default:
                            break;
                    }
                    m_stack.Children.Add(CreateMessage(kv.Key, kv.Value, m_messageCollection[kv.Value].Count()));
                }
            }
            else
            {
                foreach (var k in m_messageCollection.Keys)
                    foreach (var v in m_messageCollection[k])
                        dic.Add(v, k);

                var l = dic.OrderBy(key => key.Key);
                dic = l.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);

                foreach (var kv in dic)
                {
                    switch (kv.Value.Type)
                    {
                        case EMessageType.MESSAGE:
                            if (!m_filterMessages.IsChecked.Value)
                                continue;
                            break;
                        case EMessageType.WARNING:
                            if (!m_filterWarnings.IsChecked.Value)
                                continue;
                            break;
                        case EMessageType.ERROR:
                            if (!m_filterErrors.IsChecked.Value)
                                continue;
                            break;
                        default:
                            break;
                    }
                    m_stack.Children.Add(CreateMessage(kv.Key, kv.Value, null));
                }
            }

            m_stack.UpdateLayout();
            m_scroll.ChangeView(0, double.MaxValue, 1);
        }

        internal void ClearOutput()
        {
            m_messageCollection.Clear();
            m_stack.Children.Clear();

            m_filterMessages.Label = $"  Messages";
            m_filterWarnings.Label = $"  Warnings";
            m_filterErrors.Label = $"  Errors";
        }
    }
}
