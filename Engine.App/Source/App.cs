namespace Engine;

/// <summary>Plugin contract for extending the application (insert resources, add systems, etc.).</summary>
public interface IPlugin
{
    /// <summary>Called once during app setup to configure the app/world.</summary>
    void Build(App app);
}

/// <summary>Abstraction for driving the main loop. Implemented by window backends (e.g., SDL) or editors.</summary>
public interface IMainLoopDriver
{
    /// <summary>Runs the application loop, invoking frameStep once per frame until the loop ends.</summary>
    void Run(Action frameStep);
}

/// <summary>Central application object holding the World and execution Schedule; supports plugin composition.</summary>
public sealed class App
{
    /// <summary>Shared global state and resources.</summary>
    public World World { get; } = new();
    /// <summary>Holds systems grouped by stage and runs them on demand.</summary>
    public Schedule Schedule { get; } = new();

    private readonly List<IPlugin> _plugins = new();

    public App(Config? config = null)
    {
        World.InsertResource(config ?? Config.GetDefault());
    }

    /// <summary>Adds and builds a single plugin.</summary>
    public App AddPlugin(IPlugin plugin)
    {
        _plugins.Add(plugin);
        plugin.Build(this);
        return this;
    }

    /// <summary>Registers a system to a given stage.</summary>
    public App AddSystem(Stage stage, SystemFn system)
    {
        Schedule.AddSystem(stage, system);
        return this;
    }

    /// <summary>Inserts or replaces a world resource value.</summary>
    public App InsertResource<T>(T value) where T : notnull
    {
        World.InsertResource(value);
        return this;
    }

    /// <summary>Runs Startup once, then per-frame stages using the injected main loop driver.</summary>
    public void Run()
    {
        // The loop driver is provided by a window/editor plugin (e.g., Engine.Window's AppWindowPlugin)
        var loop = World.Resource<IMainLoopDriver>();

        Schedule.RunStage(Stage.Startup, World);

        loop.Run(() =>
        {
            Schedule.RunStage(Stage.First, World);
            Schedule.RunStage(Stage.PreUpdate, World);
            Schedule.RunStage(Stage.Update, World);
            Schedule.RunStage(Stage.PostUpdate, World);
            Schedule.RunStage(Stage.Render, World);
            Schedule.RunStage(Stage.Last, World);
        });

        // App no longer disposes window-specific resources; window/editors own their lifecycles.
    }
}
