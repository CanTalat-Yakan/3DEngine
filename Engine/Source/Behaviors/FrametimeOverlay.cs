using ImGuiNET;

namespace Engine;

/// <summary>Simple HUD overlay that displays current and peak FPS for the last second.</summary>
[Behavior]
public struct FrametimeOverlay
{
    private static double _highestFps;

    /// <summary>Draws the FPS overlay in an ImGui window each render frame.</summary>
    [OnRender]
    public static void Draw(BehaviorContext ctx)
    {
        var delta = ctx.Time.DeltaSeconds;
        var fps = 1.0 / delta;
        if (ctx.Time.ElapsedSeconds % 1.0 < delta) _highestFps = 0;
        if (fps > _highestFps) _highestFps = fps;
        Console.WriteLine($"FPS: {fps}");
        ImGui.Begin("HUD");
        ImGui.Text($"FPS: {fps:0}");
        ImGui.Text($"HighestFPS: {_highestFps:0}");
        ImGui.Text($"Delta: {delta:0.000}");
        ImGui.End();
    }
}

