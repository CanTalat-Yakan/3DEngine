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
    private static readonly ILogger Logger = Log.Category("Engine.App");

    /// <summary>Shared global state and resources.</summary>
    public World World { get; } = new();
    /// <summary>Holds systems grouped by stage and runs them on demand.</summary>
    public Schedule Schedule { get; } = new();

    private readonly List<IPlugin> _plugins = new();
    private ulong _frameCount;

    public App(Config? config = null)
    {
        // Initialize file logger early so all subsequent logs are captured to disk.
        var logPath = Path.Combine(AppContext.BaseDirectory, "Engine.log");
        FileLoggerProvider.Initialize(logPath);

        Log.PrintStartupBanner();
        Logger.Info("Creating App instance...");

        var cfg = config ?? Config.GetDefault();
        World.InsertResource(cfg);

        Logger.Info($"Config: window=\"{cfg.WindowData.Title}\" size={cfg.WindowData.Width}x{cfg.WindowData.Height} graphics={cfg.Graphics} command={cfg.WindowCommand}");
        Logger.Info("App instance created successfully.");
    }

    /// <summary>Adds and builds a single plugin.</summary>
    public App AddPlugin(IPlugin plugin)
    {
        var pluginName = plugin.GetType().Name;
        Logger.Info($"Adding plugin: {pluginName}...");
        _plugins.Add(plugin);
        try
        {
            plugin.Build(this);
            Logger.Info($"Plugin built: {pluginName} (total plugins: {_plugins.Count})");
        }
        catch (Exception ex)
        {
            Logger.Error($"Plugin '{pluginName}' failed during Build()", ex);
            throw;
        }
        return this;
    }

    /// <summary>Registers a system to a given stage.</summary>
    public App AddSystem(Stage stage, SystemFn system)
    {
        Schedule.AddSystem(stage, system);
        Logger.Trace($"System registered to stage {stage}: {system.Method.DeclaringType?.Name ?? "?"}.{system.Method.Name}");
        return this;
    }

    /// <summary>Inserts or replaces a world resource value.</summary>
    public App InsertResource<T>(T value) where T : notnull
    {
        World.InsertResource(value);
        Logger.Trace($"Resource inserted: {typeof(T).Name}");
        return this;
    }

    /// <summary>Runs Startup once, then per-frame stages using the injected main loop driver.</summary>
    public void Run()
    {
        Logger.Info("App.Run() — Resolving main loop driver...");
        var loop = World.Resource<IMainLoopDriver>();
        Logger.Info($"Main loop driver: {loop.GetType().Name}");

        Logger.Info("Running Startup stage — one-time initialization systems...");
        Schedule.RunStage(Stage.Startup, World);
        Logger.Info("Startup stage complete.");

        Logger.Info("Entering main loop — per-frame execution begins.");
        loop.Run(() =>
        {
            _frameCount++;
            if (_frameCount <= 3 || (_frameCount % 1000 == 0))
                Logger.FrameTrace($"Frame #{_frameCount} begin");

            Schedule.RunStage(Stage.First, World);
            Schedule.RunStage(Stage.PreUpdate, World);
            Schedule.RunStage(Stage.Update, World);
            Schedule.RunStage(Stage.PostUpdate, World);
            Schedule.RunStage(Stage.Render, World);
            Schedule.RunStage(Stage.Last, World);
        });

        Logger.Info($"Main loop exited after {_frameCount} frames.");
        Logger.Info("Running Cleanup stage — teardown and resource disposal...");
        Schedule.RunStage(Stage.Cleanup, World);
        Logger.Info("Cleanup stage complete. Application shutdown finished.");
    }
}
