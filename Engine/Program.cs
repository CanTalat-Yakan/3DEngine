using SDL3;
using ImGuiNET;
using System.Numerics;

namespace Engine;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Gamepad))
        {
            SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
            return;
        }

        if (!SDL.CreateWindowAndRenderer("SDL3 + ImGui", 1280, 720, SDL.WindowFlags.Resizable, out var window, out var renderer))
        {
            SDL.LogError(SDL.LogCategory.Application, $"Error creating window and renderer: {SDL.GetError()}");
            return;
        }

        SDL.ShowWindow(window);

        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
        ImGui.StyleColorsDark();

        using var imguiRenderer = new ImGuiSdlRenderer(renderer);

        bool showDemo = true;
        bool showAnother = false;
        var clearColor = new Vector4(0.45f, 0.55f, 0.60f, 1.00f);

        bool running = true;
        while (running)
        {
            while (SDL.PollEvent(out var e))
            {
                ImGuiSdlInput.ProcessEvent(e);
                if ((SDL.EventType)e.Type == SDL.EventType.Quit)
                    running = false;
                if ((SDL.EventType)e.Type == SDL.EventType.WindowCloseRequested && e.Window.WindowID == SDL.GetWindowID(window))
                    running = false;
            }

            imguiRenderer.NewFrame(window);
            ImGui.NewFrame();

            if (showDemo)
                ImGui.ShowDemoWindow(ref showDemo);

            ImGui.Begin("Hello, world!");
            ImGui.Text("This is some useful text.");
            ImGui.Checkbox("Demo Window", ref showDemo);
            ImGui.Checkbox("Another Window", ref showAnother);
            var color3 = new Vector3(clearColor.X, clearColor.Y, clearColor.Z);
            if (ImGui.ColorEdit3("clear color", ref color3))
            {
                clearColor.X = color3.X; clearColor.Y = color3.Y; clearColor.Z = color3.Z;
            }
            ImGui.Text($"Application average {1000.0f / io.Framerate:F3} ms/frame ({io.Framerate:F1} FPS)");
            ImGui.End();

            if (showAnother)
            {
                ImGui.Begin("Another Window", ref showAnother);
                ImGui.Text("Hello from another window!");
                if (ImGui.Button("Close Me")) showAnother = false;
                ImGui.End();
            }

            ImGui.Render();
            var drawData = ImGui.GetDrawData();

            SDL.SetRenderDrawColor(renderer,
                (byte)(clearColor.X * 255),
                (byte)(clearColor.Y * 255),
                (byte)(clearColor.Z * 255),
                (byte)(clearColor.W * 255));
            SDL.RenderClear(renderer);

            imguiRenderer.RenderDrawData(drawData);

            SDL.RenderPresent(renderer);
        }

        ImGui.DestroyContext();
        SDL.DestroyRenderer(renderer);
        SDL.DestroyWindow(window);

        SDL.Quit();
    }
}