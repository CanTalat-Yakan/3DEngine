namespace Engine;

/// <summary>
/// Immutable application configuration: window properties, startup command, and graphics backend.
/// Use <c>with</c> expressions or the fluent <c>With*</c> methods to derive variants.
/// </summary>
/// <example>
/// <code>
/// // Use the canonical defaults
/// var app = new App(Config.Default);
/// </code>
/// <code>
/// // Customise with named parameters
/// var cfg = Config.GetDefault(title: "My Game", width: 1920, height: 1080);
/// </code>
/// <code>
/// // Derive a variant with fluent methods
/// var headless = Config.Default
///     .WithWindow("Headless", 1, 1)
///     .WithCommand(WindowCommand.Hide)
///     .WithGraphics(GraphicsBackend.Sdl);
/// </code>
/// </example>
public sealed record Config
{
    /// <summary>
    /// Canonical defaults: "3D Engine" 600×400 window, <see cref="GraphicsBackend.Vulkan"/> backend,
    /// <see cref="WindowCommand.Show"/> on startup.
    /// </summary>
    public static Config Default { get; } = new();

    /// <summary>Initial window properties (title, size).</summary>
    public WindowData WindowData { get; init; } = new("3D Engine", 600, 400);

    /// <summary>Window action applied on startup.</summary>
    public WindowCommand WindowCommand { get; init; } = WindowCommand.Show;

    /// <summary>Desired graphics backend for the application window.</summary>
    public GraphicsBackend Graphics { get; init; } = GraphicsBackend.Vulkan;

    /// <summary>Builds a configuration with the specified parameters. All have sensible defaults.</summary>
    /// <param name="title">Window title bar text.</param>
    /// <param name="width">Window width in pixels.</param>
    /// <param name="height">Window height in pixels.</param>
    /// <param name="windowCommand">Window action applied on startup.</param>
    /// <param name="graphics">Graphics backend to use.</param>
    /// <returns>A new <see cref="Config"/> instance with the specified settings.</returns>
    public static Config GetDefault(
        string title = "3D Engine",
        int width = 600,
        int height = 400,
        WindowCommand windowCommand = WindowCommand.Show,
        GraphicsBackend graphics = GraphicsBackend.Vulkan)
        => new()
        {
            WindowData = new(title, width, height),
            WindowCommand = windowCommand,
            Graphics = graphics,
        };

    /// <summary>Returns a copy with the provided window properties.</summary>
    /// <param name="title">Window title bar text.</param>
    /// <param name="width">Window width in pixels.</param>
    /// <param name="height">Window height in pixels.</param>
    /// <returns>A new <see cref="Config"/> with updated window data.</returns>
    public Config WithWindow(string title, int width, int height)
        => this with { WindowData = new(title, width, height) };

    /// <summary>Returns a copy with the provided window data.</summary>
    /// <param name="windowData">The window properties to apply.</param>
    /// <returns>A new <see cref="Config"/> with the updated window data.</returns>
    public Config WithWindow(WindowData windowData)
        => this with { WindowData = windowData };

    /// <summary>Returns a copy with a different startup window command.</summary>
    /// <param name="command">The <see cref="WindowCommand"/> to apply on startup.</param>
    /// <returns>A new <see cref="Config"/> with the updated command.</returns>
    public Config WithCommand(WindowCommand command)
        => this with { WindowCommand = command };

    /// <summary>Returns a copy with a different graphics backend.</summary>
    /// <param name="backend">The <see cref="GraphicsBackend"/> to use.</param>
    /// <returns>A new <see cref="Config"/> with the updated backend.</returns>
    public Config WithGraphics(GraphicsBackend backend)
        => this with { Graphics = backend };

    /// <summary>Human-readable summary for diagnostics and logging.</summary>
    public override string ToString()
        => $"Config {{ Window=\"{WindowData.Title}\" {WindowData.Width}x{WindowData.Height}, Graphics={Graphics}, Command={WindowCommand} }}";
}

/// <summary>
/// Immutable window properties with input validation.
/// Title defaults to "3D Engine" if blank; dimensions are clamped to a minimum of 1.
/// </summary>
public readonly record struct WindowData
{
    /// <summary>Window title bar text.</summary>
    public string Title { get; }

    /// <summary>Window width in pixels (≥ 1).</summary>
    public int Width { get; }

    /// <summary>Window height in pixels (≥ 1).</summary>
    public int Height { get; }

    /// <summary>Creates a new <see cref="WindowData"/> with the specified properties.</summary>
    /// <param name="title">Window title bar text. If blank, defaults to <c>"3D Engine"</c>.</param>
    /// <param name="width">Window width in pixels. Clamped to a minimum of 1.</param>
    /// <param name="height">Window height in pixels. Clamped to a minimum of 1.</param>
    public WindowData(string title, int width, int height)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "3D Engine" : title;
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
    }
}

/// <summary>Window action applied on application startup.</summary>
public enum WindowCommand
{
    /// <summary>Window starts hidden.</summary>
    Hide = 0,
    /// <summary>Window starts in normal (restored) state.</summary>
    Normal = 1,
    /// <summary>Window starts minimized.</summary>
    Minimize = 2,
    /// <summary>Window starts maximized.</summary>
    Maximize = 3,
    /// <summary>Window is shown (platform default placement).</summary>
    Show = 4,
    /// <summary>Window is restored from minimized/maximized state.</summary>
    Restore = 5,
}

/// <summary>Graphics backend selector for the application window.</summary>
public enum GraphicsBackend
{
    /// <summary>SDL software renderer - simple, portable, no GPU required.</summary>
    Sdl = 0,
    /// <summary>Vulkan GPU-accelerated rendering.</summary>
    Vulkan = 1,
}
