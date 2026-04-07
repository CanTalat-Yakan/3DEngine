using System.Numerics;
using ImGuiNET;
using SDL3;

namespace Engine;

/// <summary>Sets up ImGui, schedules NewFrame/Render systems, and renders via ImGuiRenderer.</summary>
/// <remarks>
/// <para>
/// Creates the ImGui context, configures dark style and keyboard/gamepad navigation, then
/// registers three per-frame systems:
/// <list type="number">
///   <item><description><see cref="Stage.PreUpdate"/>: sets display size, delta time, framebuffer scale, and calls <c>ImGui.NewFrame()</c>.</description></item>
///   <item><description><see cref="Stage.Render"/>: calls <c>ImGui.Render()</c> and (in SDL renderer mode) clears and presents via SDL.</description></item>
///   <item><description><see cref="Stage.Cleanup"/>: tears down the ImGui context and disposes the SDL renderer backend.</description></item>
/// </list>
/// </para>
/// <para>
/// In Vulkan mode, no <see cref="SdlImGuiRenderer"/> is created - the font atlas is built
/// but not uploaded to an SDL texture; the Vulkan ImGui render node handles GPU upload.
/// </para>
/// </remarks>
/// <seealso cref="SdlImGuiRenderer"/>
/// <seealso cref="SdlImGuiInput"/>
public sealed class SdlImGuiPlugin : IPlugin
{
    /// <inheritdoc />
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

        logger.Info($"ImGui initialized - display size: {sdlWindow.Width}x{sdlWindow.Height}");

        if (sdlWindow.Renderer != IntPtr.Zero)
        {
            logger.Info("ImGui using SDL software renderer backend.");
            var renderer = new SdlImGuiRenderer(sdlWindow.Renderer);
            app.World.InsertResource(renderer);
        }
        else
        {
            // Vulkan mode: no SDL renderer, but ImGui still needs the font atlas built.
            // Don't clear tex data - the Vulkan ImGui render node will upload it to GPU.
            logger.Info("ImGui using Vulkan mode - building font atlas only (no SDL renderer).");
            var io2 = ImGui.GetIO();
            io2.Fonts.GetTexDataAsRGBA32(out IntPtr _, out int _, out int _, out _);
        }

        var appWindow = app.World.Resource<AppWindow>();
        appWindow.SDLEvent += SdlImGuiInput.ProcessEvent;

        app.AddSystem(Stage.PreUpdate, new SystemDescriptor(world =>
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
            }, "SdlImGuiPlugin.PreUpdate")
            .MainThreadOnly()
            .Read<AppWindow>()
            .Read<Time>()
            .Write<SdlImGuiRenderer>());

        app.AddSystem(Stage.Render, new SystemDescriptor(world =>
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
            }, "SdlImGuiPlugin.Render")
            .MainThreadOnly()
            .Read<AppWindow>()
            .Read<ClearColor>()
            .Write<SdlImGuiRenderer>());
        
        app.AddSystem(Stage.Cleanup, new SystemDescriptor(world =>
            {
                world.Resource<AppWindow>().SDLEvent -= SdlImGuiInput.ProcessEvent;
            
                if (world.TryGetResource<SdlImGuiRenderer>(out var imguiRenderer))
                {
                    imguiRenderer.Dispose();
                    world.RemoveResource<SdlImGuiRenderer>();
                }
            
                ImGui.DestroyContext();
            }, "SdlImGuiPlugin.Cleanup")
            .MainThreadOnly()
            .Write<AppWindow>()
            .Write<SdlImGuiRenderer>());
    }
}
