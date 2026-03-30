using SDL3;

namespace Engine;

/// <summary>Owns an SDL window and renderer and provides creation/destruction lifecycle.</summary>
public sealed class SdlWindow
{
    public string Title { get; private set; }
    public int Width { get; internal set; }
    public int Height { get; internal set; }

    public nint Window { get; private set; }
    public nint Renderer { get; private set; }

    public (int W, int H) Size => (Width, Height);

    /// <summary>Creates a new SDL window. If useVulkan is true, no SDL renderer is created and the window uses the Vulkan flag.</summary>
    public SdlWindow(string title, int width, int height, bool useVulkan = false)
    {
        Title = title;
        Width = width;
        Height = height;

        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Gamepad))
        {
            SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
            throw new InvalidOperationException($"SDL init failed: {SDL.GetError()}");
        }

        if (useVulkan)
        {
            var flags = SDL.WindowFlags.Resizable | SDL.WindowFlags.Vulkan;
            var window = SDL.CreateWindow(title, width, height, flags);
            if (window == IntPtr.Zero)
            {
                SDL.LogError(SDL.LogCategory.Application, $"Error creating Vulkan window: {SDL.GetError()}");
                throw new InvalidOperationException($"SDL Vulkan window creation failed: {SDL.GetError()}");
            }
            Window = window;
            Renderer = IntPtr.Zero; // no SDL renderer in Vulkan mode
        }
        else
        {
            if (!SDL.CreateWindowAndRenderer(title, width, height, SDL.WindowFlags.Resizable, out var window, out var renderer))
            {
                SDL.LogError(SDL.LogCategory.Application, $"Error creating window and renderer: {SDL.GetError()}");
                throw new InvalidOperationException($"SDL window/renderer creation failed: {SDL.GetError()}");
            }
            Window = window;
            Renderer = renderer;
        }
    }

    /// <summary>Destroys the SDL renderer and window and quits SDL.</summary>
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