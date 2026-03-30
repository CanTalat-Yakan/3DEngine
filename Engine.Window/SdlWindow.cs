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
        var logger = Log.Category("Engine.Window");
        Title = title;
        Width = width;
        Height = height;

        logger.Info($"Initializing SDL3 (Video + Gamepad subsystems)...");
        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Gamepad))
        {
            var err = SDL.GetError();
            logger.Error($"SDL initialization failed: {err}");
            SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {err}");
            throw new InvalidOperationException($"SDL init failed: {err}");
        }
        logger.Info("SDL3 initialized successfully.");

        if (useVulkan)
        {
            logger.Info($"Creating SDL Vulkan window: \"{title}\" ({width}x{height})...");
            var flags = SDL.WindowFlags.Resizable | SDL.WindowFlags.Vulkan;
            var window = SDL.CreateWindow(title, width, height, flags);
            if (window == IntPtr.Zero)
            {
                var err = SDL.GetError();
                logger.Error($"Vulkan window creation failed: {err}");
                SDL.LogError(SDL.LogCategory.Application, $"Error creating Vulkan window: {err}");
                throw new InvalidOperationException($"SDL Vulkan window creation failed: {err}");
            }
            Window = window;
            Renderer = IntPtr.Zero; // no SDL renderer in Vulkan mode
            logger.Info($"SDL Vulkan window created (handle=0x{window:X}).");
        }
        else
        {
            logger.Info($"Creating SDL window + software renderer: \"{title}\" ({width}x{height})...");
            if (!SDL.CreateWindowAndRenderer(title, width, height, SDL.WindowFlags.Resizable, out var window, out var renderer))
            {
                var err = SDL.GetError();
                logger.Error($"Window/renderer creation failed: {err}");
                SDL.LogError(SDL.LogCategory.Application, $"Error creating window and renderer: {err}");
                throw new InvalidOperationException($"SDL window/renderer creation failed: {err}");
            }
            Window = window;
            Renderer = renderer;
            logger.Info($"SDL window created (handle=0x{window:X}), renderer (handle=0x{renderer:X}).");
        }
    }

    /// <summary>Destroys the SDL renderer and window and quits SDL.</summary>
    public void Destroy()
    {
        var logger = Log.Category("Engine.Window");
        logger.Info("Destroying SDL window and renderer...");
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
        logger.Info("SDL window destroyed and SDL subsystems shut down.");
    }
}