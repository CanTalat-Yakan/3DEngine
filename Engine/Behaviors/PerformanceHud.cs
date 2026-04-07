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
        var time = ctx.Time;
        double fps = time.Fps;

        // Reset peak every second
        if (time.ElapsedSeconds - _peakWindowStart >= 1.0)
        {
            _peakFps = 0;
            _peakWindowStart = time.ElapsedSeconds;
        }

        if (fps > _peakFps)
            _peakFps = fps;

        ImGui.Begin("Performance", ImGuiWindowFlags.NoSavedSettings);
        ImGui.Text($"FPS:       {time.SmoothedFps:0}");
        ImGui.Text($"Peak FPS:  {_peakFps:0}");
        ImGui.Text($"Frame:     {time.DeltaSeconds * 1000.0:0.00} ms");
        ImGui.Text($"Frames:    {time.FrameCount}");
        ImGui.End();
    }
}

/// <summary>Spawns a batch of entities with <see cref="EntityCounter"/> when Space is pressed.</summary>
[Behavior]
public struct StressTestSpawner
{
    private const int BatchSize = 100_000;
    private static readonly ILogger Logger = Log.Category("Engine.StressTest");

    /// <summary>Spawns a batch of entities on a single Space press (not held).</summary>
    [OnUpdate]
    public static void OnSpacePressed(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space))
            return;

        Logger.Info($"Spawning {BatchSize:N0} entities...");

        for (int i = 0; i < BatchSize; i++)
        {
            ctx.Cmd.Spawn((id, ecs) =>
            {
                ecs.Add(id, new EntityCounter());
            });
        }

        Logger.Info($"Queued {BatchSize:N0} entity spawns (will apply after PostUpdate).");
    }
}
