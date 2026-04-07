using System.Diagnostics;

namespace Engine;

/// <summary>
/// Tracks wall-clock time since start, per-frame delta, frame count, and smoothed FPS.
/// Updates during the <see cref="Stage.First"/> stage each frame.
/// </summary>
public sealed class TimePlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.Time");

    public void Build(App app)
    {
        Logger.Info("TimePlugin: Registering Time resource and frame-timing system.");
        app.World.InitResource<Time>();

        var watch = Stopwatch.StartNew();
        double lastElapsed = 0.0;

        app.AddSystem(Stage.First, new SystemDescriptor(world =>
            {
                double now = watch.Elapsed.TotalSeconds;
                double rawDelta = now - lastElapsed;
                lastElapsed = now;
            
                var time = world.Resource<Time>();
                time.Update(now, rawDelta);
            }, "TimePlugin.Update")
            .Write<Time>());
    }
}

/// <summary>
/// Frame timing data resource. Updated once per frame by <see cref="TimePlugin"/>.
/// <para>
/// Provides raw delta, max-clamped delta (guards against debugger pauses / hitches),
/// frame count, and an exponentially smoothed FPS estimate.
/// </para>
/// </summary>
public sealed class Time
{
    /// <summary>
    /// Maximum delta time in seconds. Frames longer than this are clamped to prevent
    /// physics explosions or spiral-of-death after debugger pauses.
    /// Defaults to 0.25 s (4 FPS equivalent). Set to <see cref="double.MaxValue"/> to disable.
    /// </summary>
    public double MaxDeltaSeconds { get; set; } = 0.25;

    /// <summary>Total wall-clock seconds since the app started.</summary>
    public double ElapsedSeconds { get; private set; }

    /// <summary>
    /// Seconds since previous frame, clamped to <see cref="MaxDeltaSeconds"/>.
    /// Use this for gameplay and animation.
    /// </summary>
    public double DeltaSeconds { get; private set; }

    /// <summary>Un-clamped seconds since previous frame (raw wall-clock measurement).</summary>
    public double RawDeltaSeconds { get; private set; }

    /// <summary>Total number of frames elapsed.</summary>
    public ulong FrameCount { get; private set; }

    /// <summary>
    /// Exponentially smoothed frames-per-second estimate.
    /// Uses a smoothing factor of 0.9 to dampen per-frame jitter.
    /// </summary>
    public double SmoothedFps { get; private set; }

    /// <summary>Instantaneous FPS derived from <see cref="DeltaSeconds"/>. Zero when delta is zero.</summary>
    public double Fps => DeltaSeconds > 0.0 ? 1.0 / DeltaSeconds : 0.0;

    /// <summary>Called by <see cref="TimePlugin"/> once per frame to advance timing state.</summary>
    internal void Update(double elapsedSeconds, double rawDelta)
    {
        ElapsedSeconds = elapsedSeconds;

        RawDeltaSeconds = Math.Max(0.0, rawDelta);
        DeltaSeconds = Math.Min(RawDeltaSeconds, MaxDeltaSeconds);

        FrameCount++;

        // Exponential moving average: smoothFps = lerp(smoothFps, instantFps, 0.1)
        double instantFps = DeltaSeconds > 0.0 ? 1.0 / DeltaSeconds : SmoothedFps;
        SmoothedFps = FrameCount == 1
            ? instantFps
            : SmoothedFps * 0.9 + instantFps * 0.1;
    }

    /// <summary>Human-readable summary for diagnostics.</summary>
    public override string ToString()
        => $"Time {{ Frame={FrameCount}, Elapsed={ElapsedSeconds:F2}s, Delta={DeltaSeconds * 1000.0:F2}ms, FPS={SmoothedFps:F0} }}";
}
