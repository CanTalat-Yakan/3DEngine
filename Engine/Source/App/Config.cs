namespace Engine;

/// <summary> Application configuration: window data and initial window command. </summary>
public sealed record Config
{
    /// <summary> Initial window properties. </summary>
    public WindowData WindowData { get; init; }
    /// <summary> Whether to show, minimize, maximize, etc. on startup. </summary>
    public WindowCommand WindowCommand { get; init; } = WindowCommand.Show;

    /// <summary> Builds a default configuration. </summary>
    public static Config GetDefault(
        string title = "3D Engine",
        int width = 600,
        int height = 400,
        WindowCommand windowCommand = WindowCommand.Show)
    {
        return new Config
        {
            WindowData = new WindowData(title, width, height),
            WindowCommand = windowCommand,
        };
    }

    /// <summary> Returns a copy with the provided window properties. </summary>
    public Config WithWindow(string title, int width, int height) => this with
    {
        WindowData = new WindowData(title, width, height)
    };

    /// <summary> Returns a copy with a different startup window command. </summary>
    public Config WithCommand(WindowCommand command) => this with { WindowCommand = command };
}

/// <summary> Immutable window properties. </summary>
public readonly struct WindowData
{
    public string Title { get; }
    public int Width { get; }
    public int Height { get; }

    public WindowData(string title, int width, int height)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "3D Engine" : title;
        Width = width <= 0 ? 1 : width;
        Height = height <= 0 ? 1 : height;
    }
}

/// <summary> Initial window action to apply. </summary>
public enum WindowCommand
{
    Hide = 0,
    Normal = 1,
    Minimize = 2,
    Maximize = 3,
    Show = 4,
    Restore = 5,
}
