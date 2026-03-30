using ImGuiNET;

namespace Engine;

/// <summary>HUD overlay showing real-time performance metrics: FPS, frame time, and 1-second peak.</summary>
[Behavior]
public struct PerformanceHud
{
    private static double _peakFps;
    private static double _peakWindowStart;

    /// <summary>Draws the performance overlay each render frame.</summary>
    [OnRender]
    public static void Draw(BehaviorContext ctx)
    {
        double dt = ctx.Time.DeltaSeconds;
        double fps = dt > 0 ? 1.0 / dt : 0;
        double elapsed = ctx.Time.ElapsedSeconds;

        // Reset peak every second
        if (elapsed - _peakWindowStart >= 1.0)
        {
            _peakFps = 0;
            _peakWindowStart = elapsed;
        }

        if (fps > _peakFps)
            _peakFps = fps;

        ImGui.Begin("Performance");
        ImGui.Text($"FPS:       {fps:0}");
        ImGui.Text($"Peak FPS:  {_peakFps:0}");
        ImGui.Text($"Frame:     {dt * 1000.0:0.00} ms");
        ImGui.End();
    }
}


