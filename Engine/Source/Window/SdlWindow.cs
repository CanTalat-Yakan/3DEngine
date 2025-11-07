using SDL3;

namespace Engine;

public sealed class SdlWindow
{
    public string Title { get; private set; }
    public int Width { get; internal set; }
    public int Height { get; internal set; }

    public nint Window { get; private set; }
    public nint Renderer { get; private set; }

    public (int W, int H) Size => (Width, Height);

    public SdlWindow(string title, int width, int height)
    {
        Title = title;
        Width = width;
        Height = height;

        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Gamepad))
        {
            SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
            throw new InvalidOperationException($"SDL init failed: {SDL.GetError()}");
        }

        if (!SDL.CreateWindowAndRenderer(title, width, height, SDL.WindowFlags.Resizable, out var window, out var renderer))
        {
            SDL.LogError(SDL.LogCategory.Application, $"Error creating window and renderer: {SDL.GetError()}");
            throw new InvalidOperationException($"SDL window/renderer creation failed: {SDL.GetError()}");
        }

        Window = window;
        Renderer = renderer;
    }

    public void Destroy()
    {
        if (Renderer != IntPtr.Zero)
        {
            SDL.DestroyRenderer(Renderer);
            Renderer = IntPtr.Zero;
        }
        if (Window != IntPtr.Zero)
        {
            SDL.DestroyWindow(Window);
            Window = IntPtr.Zero;
        }
        SDL.Quit();
    }
}