using System.Numerics;
using ImGuiNET;
using SDL3;

namespace Engine;

/// <summary>Sets up ImGui, schedules NewFrame/Render systems, and renders via ImGuiRenderer.</summary>
public sealed class SdlImGuiPlugin : IPlugin
{
    private sealed class ImGuiState
    {
        public bool ShowDemo;
    }

    /// <summary>Creates ImGui context/resources and wires per-frame new-frame/render systems.</summary>
    public void Build(App app)
    {
        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.NavEnableGamepad;
        ImGui.StyleColorsDark();

        var sdlWindow = app.World.Resource<AppWindow>().Sdl;
        // Initialize IO display size immediately to avoid ImGui assertion before first NewFrame
        io.DisplaySize = new Vector2(Math.Max(1, sdlWindow.Width), Math.Max(1, sdlWindow.Height));
        io.DeltaTime = 1f / 60f;
        // Only create the SDL-based ImGuiRenderer if an SDL renderer exists (non-Vulkan mode)
        if (sdlWindow.Renderer != IntPtr.Zero)
        {
            var renderer = new SdlImGuiRenderer(sdlWindow.Renderer);
            app.World.InsertResource(renderer);
        }

        app.World.InsertResource(new ImGuiState());

        app.AddSystem(Stage.PreUpdate, (world) =>
        {
            var appWindow = world.Resource<AppWindow>();
            var io = ImGui.GetIO();
            // Safeguard: ensure non-zero display size to avoid ImGui assertion
            int w = Math.Max(1, appWindow.Sdl.Width);
            int h = Math.Max(1, appWindow.Sdl.Height);
            io.DisplaySize = new Vector2(w, h);
            io.DeltaTime = world.Resource<Time>().DeltaSeconds > 0
                ? (float)world.Resource<Time>().DeltaSeconds
                : 1f / 60f;
            ImGui.NewFrame();
            if (world.TryResource<SdlImGuiRenderer>() is { } imguiRenderer)
            {
                imguiRenderer.NewFrame(appWindow.Sdl.Window);
            }
        });

        app.AddSystem(Stage.Update, (world) =>
        {
            var state = world.Resource<ImGuiState>();
            if (state.ShowDemo) ImGui.ShowDemoWindow(ref state.ShowDemo);
        });

        app.AddSystem(Stage.Render, (world) =>
        {
            var sdl = world.Resource<AppWindow>().Sdl;
            if (sdl.Renderer == IntPtr.Zero) return; // Vulkan mode: don't use SDL renderer path
            var imGuiRenderer = world.Resource<SdlImGuiRenderer>();
            var clear = world.Resource<ClearColor>().Value;

            ImGui.Render();
            var drawData = ImGui.GetDrawData();

            SDL.SetRenderDrawColor(sdl.Renderer,
                (byte)(clear.X * 255),
                (byte)(clear.Y * 255),
                (byte)(clear.Z * 255),
                (byte)(clear.W * 255));
            SDL.RenderClear(sdl.Renderer);

            imGuiRenderer.RenderDrawData(drawData);
            SDL.RenderPresent(sdl.Renderer);
        });
        
        app.AddSystem(Stage.Cleanup, (world) =>
        {
            if (world.TryResource<SdlImGuiRenderer>() is { } imguiRenderer)
            {
                imguiRenderer.Dispose();
                world.RemoveResource<SdlImGuiRenderer>();
            }

            ImGui.DestroyContext();
        });
    }
}
