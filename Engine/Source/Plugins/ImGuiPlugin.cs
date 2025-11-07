using System.Numerics;
using ImGuiNET;
using SDL3;

namespace Engine;

public sealed class ImGuiPlugin : IPlugin
{
    private sealed class ImGuiState { public bool ShowDemo = false; }

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

        app.AddSystem(Stage.PreUpdate, (World w) =>
        {
            var aw = w.Resource<AppWindow>();
            w.Resource<ImGuiRenderer>().NewFrame(aw.SdlWindow.Window);
            ImGui.NewFrame();
        });

        app.AddSystem(Stage.Update, (World w) =>
        {
            var state = w.Resource<ImGuiState>();
            if (state.ShowDemo) ImGui.ShowDemoWindow(ref state.ShowDemo);
        });

        app.AddSystem(Stage.Render, (World w) =>
        {
            var sdl = w.Resource<AppWindow>().SdlWindow;
            var imguiRenderer = w.Resource<ImGuiRenderer>();
            var clear = w.Resource<ClearColor>().Value;

            ImGui.Render();
            var drawData = ImGui.GetDrawData();

            SDL.SetRenderDrawColor(sdl.Renderer,
                (byte)(clear.X * 255),
                (byte)(clear.Y * 255),
                (byte)(clear.Z * 255),
                (byte)(clear.W * 255));
            SDL.RenderClear(sdl.Renderer);

            imguiRenderer.RenderDrawData(drawData);
            SDL.RenderPresent(sdl.Renderer);
        });
    }
}

public sealed class ClearColorPlugin : IPlugin
{
    public void Build(App app)
    {
        if (!app.World.ContainsResource<ClearColor>())
            app.World.InsertResource(new ClearColor(new Vector4(0.45f, 0.55f, 0.60f, 1.00f)));
    }
}

public readonly struct ClearColor
{
    public readonly Vector4 Value;
    public ClearColor(Vector4 value) { Value = value; }
}

