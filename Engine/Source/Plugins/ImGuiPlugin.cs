using System.Numerics;
using ImGuiNET;
using SDL3;

namespace Engine;

/// <summary>Sets up ImGui, schedules NewFrame/Render systems, and renders via ImGuiRenderer.</summary>
public sealed class ImGuiPlugin : IPlugin
{
    private sealed class ImGuiState { public bool ShowDemo; }

    /// <summary>Creates ImGui context/resources and wires per-frame new-frame/render systems.</summary>
    public void Build(App app)
    {
        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.NavEnableGamepad;
        ImGui.StyleColorsDark();

        var sdlWindow = app.World.Resource<AppWindow>().SdlWindow;
        var renderer = new ImGuiRenderer(sdlWindow.Renderer);
        app.World.InsertResource(renderer);
        app.World.InsertResource(new ImGuiState());

        app.AddSystem(Stage.PreUpdate, (world) =>
        {
            var appWindow = world.Resource<AppWindow>();
            world.Resource<ImGuiRenderer>().NewFrame(appWindow.SdlWindow.Window);
            ImGui.NewFrame();
        });

        app.AddSystem(Stage.Update, (world) =>
        {
            var state = world.Resource<ImGuiState>();
            if (state.ShowDemo) ImGui.ShowDemoWindow(ref state.ShowDemo);
        });

        app.AddSystem(Stage.Render, (world) =>
        {
            var sdl = world.Resource<AppWindow>().SdlWindow;
            var imGuiRenderer = world.Resource<ImGuiRenderer>();
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
    }
}

/// <summary>Ensures a default ClearColor resource exists.</summary>
public sealed class ClearColorPlugin : IPlugin
{
    /// <summary>Inserts a default ClearColor resource if missing.</summary>
    public void Build(App app)
    {
        if (!app.World.ContainsResource<ClearColor>())
            app.World.InsertResource(new ClearColor(new Vector4(0.45f, 0.55f, 0.60f, 1.00f)));
    }
}

/// <summary>RGBA clear color used for SDL renderer background.</summary>
public readonly struct ClearColor
{
    /// <summary>RGBA color as Vector4 (0..1).</summary>
    public readonly Vector4 Value;
    public ClearColor(Vector4 value) { Value = value; }
}
