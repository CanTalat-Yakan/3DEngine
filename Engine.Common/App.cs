namespace Engine;

/// <summary>
/// Central application object holding the <see cref="World"/> and execution <see cref="Schedule"/>.
/// Supports Bevy-style plugin composition: add plugins, register systems, insert resources,
/// then call <see cref="Run"/> to enter the main loop.
/// </summary>
public sealed class App : IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.Application");

    /// <summary>Shared global state and resources.</summary>
    public World World { get; } = new();

    /// <summary>Holds systems grouped by stage and runs them on demand.</summary>
    public Schedule Schedule { get; } = new();

    /// <summary>Total frames executed since <see cref="Run"/> was called.</summary>
    public ulong FrameCount => _frameCount;

    private readonly Dictionary<Type, IPlugin> _plugins = new();
    private ulong _frameCount;
    private bool _disposed;

    // ── Construction ───────────────────────────────────────────────────

    public App(Config? config = null)
    {
        // Initialize file logger early so all subsequent logs are captured to disk.
        var logPath = Path.Combine(AppContext.BaseDirectory, "Engine.log");
        FileLoggerProvider.Initialize(logPath);

        Log.PrintStartupBanner();
        Logger.Info("Creating App instance...");

        var cfg = config ?? Config.Default;
        World.InsertResource(cfg);
        World.InsertResource(Schedule.Diagnostics);

        Logger.Info($"{cfg}");
        Logger.Info("App instance created successfully.");
    }

    // ── Plugin registration ────────────────────────────────────────────

    /// <summary>
    /// Adds and builds a plugin. Each concrete plugin type can only be added once;
    /// duplicate registrations are silently skipped.
    /// </summary>
    public App AddPlugin(IPlugin plugin)
    {
        var pluginType = plugin.GetType();
        var pluginName = pluginType.Name;

        if (_plugins.ContainsKey(pluginType))
        {
            Logger.Trace($"Plugin already registered, skipping: {pluginName}");
            return this;
        }

        Logger.Info($"Adding plugin: {pluginName}...");
        _plugins[pluginType] = plugin;

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

    /// <summary>Checks whether a plugin of type <typeparamref name="T"/> has been registered.</summary>
    public bool HasPlugin<T>() where T : IPlugin
        => _plugins.ContainsKey(typeof(T));

    /// <summary>Number of plugins currently registered.</summary>
    public int PluginCount => _plugins.Count;

    /// <summary>Snapshot of all registered plugin types.</summary>
    public IReadOnlyCollection<Type> Plugins => _plugins.Keys.ToArray();

    // ── System registration ────────────────────────────────────────────

    /// <summary>Registers a system to a given stage.</summary>
    public App AddSystem(Stage stage, SystemFn system)
    {
        Schedule.AddSystem(stage, system);
        Logger.Trace($"System registered to stage {stage}: {system.Method.DeclaringType?.Name ?? "?"}.{system.Method.Name}");
        return this;
    }

    /// <summary>Registers a system with a Bevy-style <c>run_if</c> condition to a given stage.</summary>
    public App AddSystem(Stage stage, SystemFn system, Func<World, bool> runCondition)
    {
        Schedule.AddSystem(stage, system, runCondition);
        Logger.Trace($"System registered to stage {stage} (conditional): {system.Method.DeclaringType?.Name ?? "?"}.{system.Method.Name}");
        return this;
    }

    /// <summary>Registers a fully configured system descriptor to a stage.</summary>
    public App AddSystem(Stage stage, SystemDescriptor descriptor)
    {
        Schedule.AddSystem(stage, descriptor);
        Logger.Trace($"System descriptor registered to stage {stage}: {descriptor.Name}");
        return this;
    }

    // ── Resource helpers ───────────────────────────────────────────────

    /// <summary>Inserts or replaces a world resource value.</summary>
    public App InsertResource<T>(T value) where T : notnull
    {
        World.InsertResource(value);
        Logger.Trace($"Resource inserted: {typeof(T).Name}");
        return this;
    }

    /// <summary>
    /// Returns the existing resource of type T, or inserts <paramref name="value"/> and returns it.
    /// Atomic — safe for concurrent callers.
    /// </summary>
    public T GetOrInsertResource<T>(T value) where T : notnull
        => World.GetOrInsertResource(value);

    /// <summary>
    /// Returns the existing resource of type T, or creates a default instance via <c>new T()</c>,
    /// inserts it, and returns it.
    /// </summary>
    public T InitResource<T>() where T : notnull, new()
        => World.InitResource<T>();

    // ── Execution ──────────────────────────────────────────────────────

    /// <summary>
    /// Runs Startup once, enters the per-frame main loop, then runs Cleanup on exit.
    /// The main loop is driven by the <see cref="IMainLoopDriver"/> resource.
    /// </summary>
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

            foreach (var stage in StageOrder.FrameStages())
                Schedule.RunStage(stage, World);
        });

        Logger.Info($"Main loop exited after {_frameCount} frames.");
        Logger.Info("Running Cleanup stage — teardown and resource disposal...");
        Schedule.RunStage(Stage.Cleanup, World);

        // Tear down platform resources (e.g., SDL window) *after* Cleanup systems
        // have released GPU resources that depend on the window/surface.
        Logger.Info("Shutting down main loop driver (platform teardown)...");
        loop.Shutdown();

        // Dispose all IDisposable resources as a safety net.
        World.Dispose();
        Logger.Info("Cleanup stage complete. Application shutdown finished.");
    }

    // ── Lifecycle ──────────────────────────────────────────────────────

    /// <summary>Disposes the <see cref="World"/> and all its resources.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        World.Dispose();
        Logger.Trace("App disposed.");
    }
}

