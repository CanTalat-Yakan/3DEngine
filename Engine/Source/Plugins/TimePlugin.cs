namespace Engine;

public sealed class TimePlugin : IPlugin
{
    private sealed class TimeState
    {
        public System.Diagnostics.Stopwatch Watch = System.Diagnostics.Stopwatch.StartNew();
        public double LastElapsedSeconds;
    }

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

public sealed class Time
{
    public double ElapsedSeconds { get; internal set; }
    public double DeltaSeconds { get; internal set; }
}

