using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System;

namespace Editor.Controller
{
    public enum MessageType
    {
        Message,
        Warning,
        Error
    }

    internal struct MessageInfo
    {
        public MessageType Type;
        public string Script;
        public string Method;
        public int? Line;
        public string Message;

        public readonly string GetInfo()
        {
            if (Script is not null)
                return Method is not null
                   ? Script.Split("\\").Last() + $":{Line} ({Method})"
                   : Script.Split("\\").Last() + $":{Line}";

            return string.Empty;
        }
    }

    public class Output
    {
        internal AppBarToggleButton ClearPlay;

        private static Dictionary<MessageInfo, List<DateTime>> s_messageCollection = new();

        private static TextBlock s_status;
        private static Viewbox s_statusIcon;
        private static StackPanel s_stack;
        private static ScrollViewer s_scroll;
        private static AppBarToggleButton s_collapse;
        private static AppBarToggleButton s_filterMessages;
        private static AppBarToggleButton s_filterWarnings;
        private static AppBarToggleButton s_filterErrors;
        private static AppBarToggleButton s_pauseError;

        private static Dictionary<DateTime, MessageInfo> _dic = new();

        public Output(StackPanel stack, ScrollViewer scroll, AppBarToggleButton collapse, AppBarToggleButton filterMessages, AppBarToggleButton filterWarnings, AppBarToggleButton filterErrors, AppBarToggleButton pauseError, AppBarToggleButton clearPlay)
        {
            // Assign local variables
            s_stack = stack;
            s_scroll = scroll;
            s_collapse = collapse;
            s_filterMessages = filterMessages;
            s_filterWarnings = filterWarnings;
            s_filterErrors = filterErrors;
            s_pauseError = pauseError;
            ClearPlay = clearPlay;

            s_status = Main.Instance.Status;
            s_statusIcon = Main.Instance.StatusIcon;
        }

        public static void Log(Engine.MessageLog log)
        {
            if (log is not null)
                // Logs a message, with a string representation of an object.
                Log(log.o?.ToString(), (MessageType)log?.type, log.line, log?.method, log?.script);
        }

        public static void Log(object o, MessageType type = MessageType.Message, [CallerLineNumber] int line = 0, [CallerMemberName] string method = null, [CallerFilePath] string script = null) =>
            // Logs a message, with a string representation of an object.
            Log(o.ToString(), type, line, method, script);

        public static void Log(string s, MessageType type = MessageType.Message, [CallerLineNumber] int line = 0, [CallerMemberName] string method = null, [CallerFilePath] string script = null)
        {
            // Create a new instance of SMessageInfo with the given properties.
            MessageInfo message = new()
            {
                Script = script,
                Method = method,
                Line = line,
                Message = s,
                Type = type
            };

            // Check if s_status is null and if it is, return,
            // to prevent setting the status if it isn't initialized.
            if (s_status is null)
                return;

            // Check if the "Pause on Error" checkbox is checked and if the message type is an error.
            if (s_pauseError.IsChecked.Value)
                if (type == MessageType.Error)
                    // Pause the Play mode with the message type error.
                    Main.Instance.PlayerControl.Pause();

            // Check if the message is not already in the message collection.
            if (!s_messageCollection.ContainsKey(message))
                // Add a new message to the collection.
                s_messageCollection.Add(message, new List<DateTime> { DateTime.Now });
            else
                // Find the message in the collection and add a new timestamp.
                s_messageCollection[message].Add(DateTime.Now);

            // Set the message to the Status bar.
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
                    case MessageType.Message:
                        numMessages += kv.Value.Count;
                        break;
                    case MessageType.Warning:
                        numWarnings += kv.Value.Count;
                        break;
                    case MessageType.Error:
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
                    case MessageType.Message:
                        // Check if the "Message" filter is checked.
                        if (!s_filterMessages.IsChecked.Value)
                            // If it's not, continue to the next iteration.
                            continue;
                        break;
                    case MessageType.Warning:
                        // Check if the "Warning" filter is checked.
                        if (!s_filterWarnings.IsChecked.Value)
                            // If it's not, continue to the next iteration.
                            continue;
                        break;
                    case MessageType.Error:
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

        private static void SetStatus(MessageInfo m)
        {
            if (!string.IsNullOrEmpty(m.Message))
                s_status.Text = m.Message.Split("\n")[0];

            if (m.Type == MessageType.Warning)
                s_statusIcon.Child = new FontIcon() { Glyph = "\uE7BA" };
            else if (m.Type == MessageType.Message)
                s_statusIcon.Child = new SymbolIcon() { Symbol = Symbol.Message };
            else if (m.Type == MessageType.Error)
                s_statusIcon.Child = new SymbolIcon() { Symbol = Symbol.ReportHacked };
        }

        private static UIElement CreateMessage(DateTime d, MessageInfo m, int? i)
        {
            // Content of the message.
            StackPanel stack = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top, Spacing = 10, Margin = new(10, 0, 50, 0), Padding = new(5) };
            Viewbox viewbox = new() { Width = 14, Height = 14 };

            if (m.Type == MessageType.Warning)
                viewbox.Child = new FontIcon() { Glyph = "\uE7BA" };
            else if (m.Type == MessageType.Message)
                viewbox.Child = new SymbolIcon() { Symbol = Symbol.Message };
            else if (m.Type == MessageType.Error)
                viewbox.Child = new SymbolIcon() { Symbol = Symbol.ReportHacked };

            stack.Children.Add(viewbox);
            stack.Children.Add(new TextBlock() { Text = "[" + d.ToShortTimeString().ToString() + "]" });
            stack.Children.Add(new TextBlock() { Text = m.Message });

            // The flyout when clicked on the message.
            StackPanel stackFlyout = new() { Orientation = Orientation.Vertical };
            if (!string.IsNullOrEmpty(m.Script))
                stackFlyout.Children.Add(new TextBlock() { Text = m.GetInfo() + "\n" });

            stackFlyout.Children.Add(new MarkdownTextBlock() { Text = m.Message, TextWrapping = TextWrapping.Wrap, Width = 800, Padding = new(8) });

            if (!string.IsNullOrEmpty(m.Script))
            {
                var filePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), m.Script);

                HyperlinkButton hyperlinkButton = new() { Content = filePath + ":" + m.Line, HorizontalContentAlignment = HorizontalAlignment.Stretch, Foreground = new SolidColorBrush(Colors.CadetBlue) };
                if (File.Exists(filePath))
                    hyperlinkButton.Click += (s, e) => Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });

                stackFlyout.Children.Add(hyperlinkButton);
            }

            Flyout flyout = new Flyout() { OverlayInputPassThroughElement = stack, Content = stackFlyout };

            Style style = new Style { TargetType = typeof(FlyoutPresenter) };
            style.Setters.Add(new Setter(FlyoutPresenter.MinWidthProperty, "830"));
            flyout.FlyoutPresenterStyle = style;

            // Create main grid that gets returned
            Grid grid = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
            grid.Children.Add(stack);

            // If there is a count the number gets shown on the right.
            if (i is not null)
                grid.Children.Add(new TextBlock() { Margin = new(0, 0, 10, 0), Padding = new(5), MinWidth = 25, HorizontalAlignment = HorizontalAlignment.Right, Text = i.ToString() });

            // Set flyout to button that stretches along the grid.
            grid.Children.Add(new Button()
            {
                Flyout = flyout,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(
                    m.Type == MessageType.Message
                        ? s_stack.Children.Count() % 2 == 0
                            ? Colors.Transparent
                            : Windows.UI.Color.FromArgb(50, 10, 10, 10)
                        : m.Type == MessageType.Error
                            ? Windows.UI.Color.FromArgb(88, 255, 0, 0)
                            : Windows.UI.Color.FromArgb(88, 255, 255, 0))
            }.AddToolTip(string.IsNullOrEmpty(m.Script) 
                ? m.Type.ToString() 
                : m.Script ));

            // return new message.
            return grid;
        }
    }
}
