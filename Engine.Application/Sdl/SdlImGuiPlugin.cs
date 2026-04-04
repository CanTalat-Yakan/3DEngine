using System.Numerics;
using ImGuiNET;
using SDL3;

namespace Engine;

/// <summary>Sets up ImGui, schedules NewFrame/Render systems, and renders via ImGuiRenderer.</summary>
public sealed class SdlImGuiPlugin : IPlugin
{
    public void Build(App app)
    {
        var logger = Log.Category("Engine.ImGui");
        logger.Info("SdlImGuiPlugin: Creating ImGui context...");
        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.NavEnableGamepad;
        ImGui.StyleColorsDark();

        var sdlWindow = app.World.Resource<AppWindow>().Sdl;
        io.DisplaySize = new Vector2(Math.Max(1, sdlWindow.Width), Math.Max(1, sdlWindow.Height));
        io.DeltaTime = 1f / 60f;

        logger.Info($"ImGui initialized — display size: {sdlWindow.Width}x{sdlWindow.Height}");

        if (sdlWindow.Renderer != IntPtr.Zero)
        {
            logger.Info("ImGui using SDL software renderer backend.");
            var renderer = new SdlImGuiRenderer(sdlWindow.Renderer);
            app.World.InsertResource(renderer);
        }
        else
        {
            // Vulkan mode: no SDL renderer, but ImGui still needs the font atlas built.
            // Don't clear tex data — the Vulkan ImGui render node will upload it to GPU.
            logger.Info("ImGui using Vulkan mode — building font atlas only (no SDL renderer).");
            var io2 = ImGui.GetIO();
            io2.Fonts.GetTexDataAsRGBA32(out IntPtr _, out int _, out int _, out _);
        }

        var appWindow = app.World.Resource<AppWindow>();
        appWindow.SDLEvent += SdlImGuiInput.ProcessEvent;

        app.AddSystem(Stage.PreUpdate, (world) =>
        {
            var appWindow = world.Resource<AppWindow>();
            var io = ImGui.GetIO();
            int w = Math.Max(1, appWindow.Sdl.Width);
            int h = Math.Max(1, appWindow.Sdl.Height);
            io.DisplaySize = new Vector2(w, h);
            io.DeltaTime = world.Resource<Time>().DeltaSeconds > 0
                ? (float)world.Resource<Time>().DeltaSeconds
                : 1f / 60f;


            // Set framebuffer scale for Vulkan mode (SDL renderer path sets this in NewFrame)
            if (appWindow.Sdl.Renderer == IntPtr.Zero)
            {
                SDL.GetWindowSizeInPixels(appWindow.Sdl.Window, out int pxW, out int pxH);
                if (w > 0 && h > 0)
                {
                    io.DisplayFramebufferScale = new Vector2(pxW / (float)w, pxH / (float)h);
                }
            }

            ImGui.NewFrame();
            if (world.TryGetResource<SdlImGuiRenderer>(out var imguiRenderer))
            {
                imguiRenderer.NewFrame(appWindow.Sdl.Window);
            }
        });

        app.AddSystem(Stage.Render, (world) =>
        {
            var sdl = world.Resource<AppWindow>().Sdl;
            if (sdl.Renderer == IntPtr.Zero)
            {
                // Vulkan mode: still need to end the ImGui frame.
                ImGui.Render();
                return;
            }
            var imGuiRenderer = world.Resource<SdlImGuiRenderer>();
            var clear = world.Resource<ClearColor>();

            ImGui.Render();
            var drawData = ImGui.GetDrawData();

            SDL.SetRenderDrawColor(sdl.Renderer,
                (byte)(clear.R * 255),
                (byte)(clear.G * 255),
                (byte)(clear.B * 255),
                (byte)(clear.A * 255));
            SDL.RenderClear(sdl.Renderer);

            imGuiRenderer.RenderDrawData(drawData);
            SDL.RenderPresent(sdl.Renderer);
        });
        
        app.AddSystem(Stage.Cleanup, (world) =>
        {
            world.Resource<AppWindow>().SDLEvent -= SdlImGuiInput.ProcessEvent;

            if (world.TryGetResource<SdlImGuiRenderer>(out var imguiRenderer))
            {
                imguiRenderer.Dispose();
                world.RemoveResource<SdlImGuiRenderer>();
            }

            ImGui.DestroyContext();
        });
    }
}
