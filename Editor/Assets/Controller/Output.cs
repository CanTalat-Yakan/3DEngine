using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System;

using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI;

namespace Editor.Controller;

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

public enum MessageType
{
    Message,
    Warning,
    Error
}

public sealed partial class Output
{
    internal AppBarToggleButton ClearPlay;

    private static Dictionary<MessageInfo, List<DateTime>> s_messageCollection = new();
    private static Dictionary<DateTime, MessageInfo> s_dic = new();

    private static TextBlock s_status;
    private static Viewbox s_statusIcon;
    private static StackPanel s_stack;
    private static ScrollViewer s_scroll;
    private static AppBarToggleButton s_collapse;
    private static AppBarToggleButton s_filterMessages;
    private static AppBarToggleButton s_filterWarnings;
    private static AppBarToggleButton s_filterErrors;
    private static AppBarToggleButton s_pauseError;

    public Output(StackPanel stack, ScrollViewer scroll, AppBarToggleButton collapse, AppBarToggleButton filterMessages, AppBarToggleButton filterWarnings, AppBarToggleButton filterErrors, AppBarToggleButton pauseError, AppBarToggleButton clearPlay)
    {
        ClearPlay = clearPlay;

        s_stack = stack;
        s_scroll = scroll;
        s_collapse = collapse;
        s_filterMessages = filterMessages;
        s_filterWarnings = filterWarnings;
        s_filterErrors = filterErrors;
        s_pauseError = pauseError;

        s_status = Main.Instance.Status;
        s_statusIcon = Main.Instance.StatusIcon;
    }

    public static void Log(Engine.Utilities.MessageLog log)
    {
        if (log is not null)
            Log(log?.o?.ToString(), (MessageType)log?.type, log.line, log?.method, log?.script);
    }

    public static void Log(object o, MessageType type = MessageType.Message, [CallerLineNumber] int line = 0, [CallerMemberName] string method = null, [CallerFilePath] string script = null)
    {
        MessageInfo message = CreateMessageInfo(o, type, line, method, script);

        if (s_status is null)
            return;

        HandlePauseOnError(type);

        UpdateMessageCollection(message);

        SetStatus(message);

        IterateOutputMessages();
    }

    public void ClearOutput()
    {
        s_messageCollection.Clear();
        s_stack.Children.Clear();

        ResetFilterLabels();
    }
}

public sealed partial class Output
{
    private static void HandlePauseOnError(MessageType type)
    {
        if (s_pauseError.IsChecked.Value && type == MessageType.Error)
            Main.Instance.PlayerControl.Pause();
    }

    private static void UpdateMessageCollection(MessageInfo message)
    {
        if (!s_messageCollection.ContainsKey(message))
            s_messageCollection.Add(message, new List<DateTime> { DateTime.Now });
        else
            s_messageCollection[message].Add(DateTime.Now);
    }

    private static void ResetFilterLabels()
    {
        s_filterMessages.Label = "Messages";
        s_filterWarnings.Label = "Warnings";
        s_filterErrors.Label = "Errors";
    }

    private static void SetStatus(MessageInfo m)
    {
        if (!string.IsNullOrEmpty(m.Message))
            s_status.Text = m.Message.Trim().Split('\n')[0];

        if (m.Type == MessageType.Warning)
            s_statusIcon.Child = new FontIcon() { Glyph = "\uE7BA" };
        else if (m.Type == MessageType.Message)
            s_statusIcon.Child = new SymbolIcon() { Symbol = Symbol.Message };
        else if (m.Type == MessageType.Error)
            s_statusIcon.Child = new SymbolIcon() { Symbol = Symbol.ReportHacked };
    }

    internal static void IterateOutputMessages()
    {
        s_stack.Children.Clear();

        int numMessages = 0;
        int numWarnings = 0;
        int numErrors = 0;

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
            }
        }

        UpdateFilterLabels(numMessages, numWarnings, numErrors);

        s_dic.Clear();

        foreach (var k in s_messageCollection.Keys)
        {
            if (s_collapse.IsChecked.Value)
                s_dic.Add(s_messageCollection[k].Last(), k);
            else
                foreach (var v in s_messageCollection[k])
                    s_dic.Add(v, k);
        }

        var l = s_dic.OrderBy(key => key.Key);
        s_dic = l.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);

        foreach (var kv in s_dic)
        {
            switch (kv.Value.Type)
            {
                case MessageType.Message when !s_filterMessages.IsChecked.Value:
                case MessageType.Warning when !s_filterWarnings.IsChecked.Value:
                case MessageType.Error when !s_filterErrors.IsChecked.Value:
                    continue;
            }

            s_stack.Children.Add(CreateMessageElement(kv.Key, kv.Value));
        }

        s_stack.UpdateLayout();
        s_scroll.ChangeView(0, double.MaxValue, 1);
    }

    private static void UpdateFilterLabels(int numMessages, int numWarnings, int numErrors)
    {
        s_filterMessages.Label = $"{numMessages} Messages";
        s_filterWarnings.Label = $"{numWarnings} Warnings";
        s_filterErrors.Label = $"{numErrors} Errors";
    }
}

public sealed partial class Output
{
    private static UIElement CreateMessageElement(DateTime d, MessageInfo m)
    {
        StackPanel stack = CreateMessageContentStack(m, d);
        StackPanel stackFlyout = CreateMessageFlyoutStack(m);

        Flyout flyout = CreateMessageFlyout(stack, stackFlyout);

        Grid grid = CreateMessageGrid(m, stack, flyout);

        return grid;
    }

    private static StackPanel CreateMessageContentStack(MessageInfo m, DateTime d)
    {
        StackPanel stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Top,
            Spacing = 10,
            Margin = new Thickness(10, 0, 50, 0),
            Padding = new Thickness(5)
        };

        stack.Children.Add(CreateMessageIcon(m));
        stack.Children.Add(new TextBlock() { Text = "[" + d.ToShortTimeString().ToString() + "]" });
        stack.Children.Add(new TextBlock() { Text = m.Message });

        return stack;
    }

    private static UIElement CreateMessageIcon(MessageInfo m)
    {
        Viewbox viewbox = new Viewbox { Width = 14, Height = 14 };

        if (m.Type == MessageType.Warning)
            viewbox.Child = new FontIcon() { Glyph = "\uE7BA" };
        else if (m.Type == MessageType.Message)
            viewbox.Child = new SymbolIcon() { Symbol = Symbol.Message };
        else if (m.Type == MessageType.Error)
            viewbox.Child = new SymbolIcon() { Symbol = Symbol.ReportHacked };

        return viewbox;
    }

    private static StackPanel CreateMessageFlyoutStack(MessageInfo m)
    {
        StackPanel stackFlyout = new StackPanel() { Orientation = Orientation.Vertical };

        if (!string.IsNullOrEmpty(m.Script))
            stackFlyout.Children.Add(new TextBlock() { Text = m.GetInfo() + "\n" });

        stackFlyout.Children.Add(new MarkdownTextBlock() { Text = m.Message, TextWrapping = TextWrapping.Wrap, Width = 800, Padding = new Thickness(8) });

        if (!string.IsNullOrEmpty(m.Script))
        {
            var filePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), m.Script);

            HyperlinkButton hyperlinkButton = new HyperlinkButton() { Content = filePath + ":" + m.Line, HorizontalContentAlignment = HorizontalAlignment.Stretch, Foreground = new SolidColorBrush(Colors.CadetBlue) };

            if (File.Exists(filePath))
                hyperlinkButton.Click += (s, e) => Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });

            stackFlyout.Children.Add(hyperlinkButton);
        }

        return stackFlyout;
    }

    private static Flyout CreateMessageFlyout(StackPanel stack, StackPanel stackFlyout)
    {
        Flyout flyout = new Flyout() { OverlayInputPassThroughElement = stack, Content = stackFlyout };

        Style style = new Style { TargetType = typeof(FlyoutPresenter) };
        style.Setters.Add(new Setter(FlyoutPresenter.MinWidthProperty, "830"));
        flyout.FlyoutPresenterStyle = style;

        return flyout;
    }

    private static Grid CreateMessageGrid(MessageInfo m, StackPanel stack, Flyout flyout)
    {
        Grid grid = new Grid() { HorizontalAlignment = HorizontalAlignment.Stretch };
        grid.Children.Add(stack);

        if (m.Type == MessageType.Message)
        {
            if (s_stack.Children.Count() % 2 == 0)
                grid.Background = new SolidColorBrush(Colors.Transparent);
            else
                grid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 10, 10, 10));
        }
        else if (m.Type == MessageType.Error)
            grid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(88, 255, 0, 0));
        else
            grid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(88, 255, 255, 0));

        Button button = CreateMessageButton(flyout);

        string tooltip = string.IsNullOrEmpty(m.Script) ? m.Type.ToString() : m.Script;
        button.AddToolTip(tooltip);

        if (s_collapse.IsChecked.Value)
            grid.Children.Add(CreateMessageCountTextBlock(m));

        grid.Children.Add(button);

        return grid;
    }

    static MessageInfo CreateMessageInfo(object o, MessageType type, int line, string method, string script) =>
        new()
        {
            Script = script,
            Method = method,
            Line = line,
            Message = o.ToString(),
            Type = type
        };

    private static TextBlock CreateMessageCountTextBlock(MessageInfo m) =>
        new()
        {
            Margin = new Thickness(0, 0, 10, 0),
            Padding = new Thickness(5),
            MinWidth = 25,
            HorizontalAlignment = HorizontalAlignment.Right,
            Text = s_messageCollection[m].Count().ToString(),
        };

    private static Button CreateMessageButton(Flyout flyout) =>
        new()
        {
            Flyout = flyout,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
}
