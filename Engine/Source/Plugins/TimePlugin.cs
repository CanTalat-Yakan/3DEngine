namespace Engine;

/// <summary> Tracks time since start and per-frame delta using a Stopwatch; updates during the First stage. </summary>
public sealed class TimePlugin : IPlugin
{
    private sealed class TimeState
    {
        public System.Diagnostics.Stopwatch Watch = System.Diagnostics.Stopwatch.StartNew();
        public double LastElapsedSeconds;
    }

    /// <summary> Inserts Time resources and updates them each First stage. </summary>
    public void Build(App app)
    {
        app.World.InsertResource(new Time());
        app.World.InsertResource(new TimeState());

        app.AddSystem(Stage.First, (World w) =>
        {
            var state = w.Resource<TimeState>();
            double now = state.Watch.Elapsed.TotalSeconds;
            double delta = now - state.LastElapsedSeconds;
            state.LastElapsedSeconds = now;

            var time = w.Resource<Time>();
            time.ElapsedSeconds = now;
            time.DeltaSeconds = delta < 0 ? 0 : delta;
        });
    }
}

/// <summary> Frame timing data resource. </summary>
public sealed class Time
{
    /// <summary> Total seconds since the app started. </summary>
    public double ElapsedSeconds { get; internal set; }
    /// <summary> Seconds since previous frame. </summary>
    public double DeltaSeconds { get; internal set; }
}
