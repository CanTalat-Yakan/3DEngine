namespace Engine;

public sealed record class Config
{
    public WindowData WindowData { get; init; }
    public WindowCommand WindowCommand { get; init; } = WindowCommand.Show;

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

    public Config WithWindow(string title, int width, int height) => this with
    {
        WindowData = new WindowData(title, width, height)
    };

    public Config WithCommand(WindowCommand command) => this with { WindowCommand = command };
}

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

public enum WindowCommand
{
    Hide = 0,
    Normal = 1,
    Minimize = 2,
    Maximize = 3,
    Show = 4,
    Restore = 5,
}
