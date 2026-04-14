using ImGuiNET;

namespace Engine;

/// <summary>HUD overlay showing real-time performance metrics: FPS, frame time, and 1-second peak.</summary>
/// <remarks>
/// Renders into an ImGui <c>"Performance"</c> window.  The peak FPS counter resets every second
/// based on <see cref="Time.ElapsedSeconds"/>.
/// </remarks>
/// <seealso cref="Time"/>
/// <seealso cref="EntityCounter"/>
[Behavior]
public struct PerformanceHud
{
    private static double _peakFps;
    private static double _peakWindowStart;

    /// <summary>Draws the performance overlay each render frame.</summary>
    /// <param name="ctx">Behavior context providing access to <see cref="Time"/> and other resources.</param>
    [OnRender]
    public static void Draw(BehaviorContext ctx)
    {
        var time = ctx.Time;
        double fps = time.Fps;
        int count = ctx.Ecs.Count<EntityCounter>();

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
        ImGui.Text($"Entities:  {count:N0}");
        ImGui.End();
    }
}

/// <summary>Tracks a per-entity tick counter and displays entity statistics in the HUD.</summary>
/// <seealso cref="PerformanceHud"/>
/// <seealso cref="StressTestSpawner"/>
[Behavior]
public struct EntityCounter
{
    /// <summary>Number of Update ticks this entity has experienced.</summary>
    public int Ticks;

    /// <summary>Increments this entity's tick counter each update.</summary>
    [OnUpdate]
    public void Tick(BehaviorContext ctx) =>
        Ticks++;
}

/// <summary>Spawns a batch of entities with <see cref="EntityCounter"/> when Space is pressed.</summary>
/// <remarks>
/// Creates <c>100_000</c> entities per press via <see cref="EcsCommands.SpawnBatch{T}(int)"/>.
/// Commands are deferred and applied after PostUpdate. The batch API pre-allocates
/// capacity to avoid resize storms and uses a single delegate allocation instead of N.
/// </remarks>
/// <seealso cref="EntityCounter"/>
/// <seealso cref="EcsCommands"/>
[Behavior]
public struct StressTestSpawner
{
    private const int BatchSize = 100_000;
    private static readonly ILogger Logger = Log.Category("Engine.StressTest");

    /// <summary>Spawns a batch of entities on a single Space press (not held).</summary>
    /// <param name="ctx">Behavior context providing input and deferred command access.</param>
    [OnUpdate]
    public static void OnSpacePressed(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space))
            return;

        Logger.Info($"Spawning {BatchSize:N0} entities...");

        ctx.Cmd.SpawnBatch<EntityCounter>(BatchSize);

        Logger.Info($"Queued {BatchSize:N0} entity spawns (will apply after PostUpdate).");
    }
}
