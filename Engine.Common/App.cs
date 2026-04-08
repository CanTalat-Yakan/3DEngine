namespace Engine;

/// <summary>
/// Central application object holding the <see cref="World"/> and execution <see cref="Schedule"/>.
/// Supports plugin composition: add plugins, register systems, insert resources,
/// then call <see cref="Run"/> to enter the main loop.
/// </summary>
/// <remarks>
/// <para>
/// <b>Lifecycle:</b> Create an <see cref="App"/>, add plugins via <see cref="AddPlugin"/>,
/// register systems via <see cref="AddSystem(Stage, SystemFn)"/>, insert resources via
/// <see cref="InsertResource{T}"/>, and finally call <see cref="Run"/>.
/// </para>
/// <para>
/// The <see cref="Run"/> method executes three phases:
/// <list type="number">
///   <item><description><see cref="Stage.Startup"/> - one-time initialization systems.</description></item>
///   <item><description>Main loop - per-frame stages (<see cref="Stage.First"/> through <see cref="Stage.Last"/>), driven by <see cref="IMainLoopDriver"/>.</description></item>
///   <item><description><see cref="Stage.Cleanup"/> - teardown and resource disposal.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var app = new App(Config.Default.WithWindow("My Game", 1280, 720));
/// app.AddPlugin(new TimePlugin())
///    .AddPlugin(new InputPlugin())
///    .AddSystem(Stage.Update, MyGameSystem);
/// app.Run();
/// </code>
/// </example>
/// <seealso cref="World"/>
/// <seealso cref="Schedule"/>
/// <seealso cref="IPlugin"/>
/// <seealso cref="Config"/>
public sealed class App : IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.Application");

    /// <summary>Shared global state and resource container for the application.</summary>
    /// <seealso cref="World"/>
    public World World { get; } = new();

    /// <summary>Holds systems grouped by <see cref="Stage"/> and executes them on demand.</summary>
    /// <seealso cref="Schedule"/>
    public Schedule Schedule { get; } = new();

    /// <summary>Total frames executed since <see cref="Run"/> was called.</summary>
    public ulong FrameCount => _frameCount;

    private readonly Dictionary<Type, IPlugin> _plugins = new();
    private ulong _frameCount;
    private bool _disposed;

    // ── Construction ───────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new <see cref="App"/> instance, configuring the file logger, startup banner,
    /// and inserting the <see cref="Config"/> and <see cref="ScheduleDiagnostics"/> resources into the <see cref="World"/>.
    /// </summary>
    /// <param name="config">
    /// Optional application configuration. When <c>null</c>, <see cref="Config.Default"/> is used
    /// (600×400 window, Vulkan backend, <see cref="WindowCommand.Show"/>).
    /// </param>
    /// <example>
    /// <code>
    /// // Use default configuration
    /// var app = new App();
    ///
    /// // Use custom configuration
    /// var app = new App(Config.GetDefault(title: "My Game", width: 1920, height: 1080));
    /// </code>
    /// </example>
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
    /// <param name="plugin">The plugin instance to register and build.</param>
    /// <returns>This <see cref="App"/> instance for fluent chaining.</returns>
    /// <exception cref="Exception">Re-throws any exception thrown by the plugin's <see cref="IPlugin.Build"/> method.</exception>
    /// <remarks>
    /// Plugins are identified by their concrete <see cref="Type"/>. Calling <c>AddPlugin</c> twice
    /// with the same plugin type is a no-op. The plugin's <see cref="IPlugin.Build"/> method is
    /// invoked immediately during this call - not deferred.
    /// </remarks>
    /// <example>
    /// <code>
    /// app.AddPlugin(new TimePlugin())
    ///    .AddPlugin(new InputPlugin())
    ///    .AddPlugin(new EcsPlugin());
    /// </code>
    /// </example>
    /// <seealso cref="IPlugin"/>
    /// <seealso cref="HasPlugin{T}"/>
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
    /// <typeparam name="T">The concrete plugin type to look up.</typeparam>
    /// <returns><c>true</c> if a plugin of type <typeparamref name="T"/> has been added; otherwise <c>false</c>.</returns>
    public bool HasPlugin<T>() where T : IPlugin
        => _plugins.ContainsKey(typeof(T));

    /// <summary>Number of plugins currently registered.</summary>
    /// <returns>The count of registered plugins.</returns>
    public int PluginCount => _plugins.Count;

    /// <summary>Snapshot of all registered plugin types at the time of the call.</summary>
    /// <returns>A read-only collection of <see cref="Type"/> objects representing each registered plugin.</returns>
    public IReadOnlyCollection<Type> Plugins => _plugins.Keys.ToArray();

    // ── System registration ────────────────────────────────────────────

    /// <summary>Registers a system delegate to a given execution stage.</summary>
    /// <param name="stage">The <see cref="Stage"/> during which the system should execute.</param>
    /// <param name="system">The system delegate to invoke each time the stage runs.</param>
    /// <returns>This <see cref="App"/> instance for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// app.AddSystem(Stage.Update, static world =>
    /// {
    ///     var time = world.Resource&lt;Time&gt;();
    ///     Console.WriteLine($"Delta: {time.DeltaSeconds:F4}s");
    /// });
    /// </code>
    /// </example>
    /// <seealso cref="SystemFn"/>
    /// <seealso cref="Stage"/>
    public App AddSystem(Stage stage, SystemFn system)
    {
        Schedule.AddSystem(stage, system);
        Logger.Trace($"System registered to stage {stage}: {system.Method.DeclaringType?.Name ?? "?"}.{system.Method.Name}");
        return this;
    }

    /// <summary>Registers a system with a <c>run_if</c> condition to a given stage.</summary>
    /// <param name="stage">The <see cref="Stage"/> during which the system should execute.</param>
    /// <param name="system">The system delegate to invoke each time the stage runs.</param>
    /// <param name="runCondition">
    /// A predicate evaluated each frame before the system runs. The system is skipped when
    /// this returns <c>false</c>.
    /// </param>
    /// <returns>This <see cref="App"/> instance for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// app.AddSystem(Stage.Update, MySystem,
    ///     BehaviorConditions.ResourceIs&lt;GameState&gt;(s => s.IsPlaying));
    /// </code>
    /// </example>
    /// <seealso cref="SystemDescriptor.RunIf"/>
    public App AddSystem(Stage stage, SystemFn system, Func<World, bool> runCondition)
    {
        Schedule.AddSystem(stage, system, runCondition);
        Logger.Trace($"System registered to stage {stage} (conditional): {system.Method.DeclaringType?.Name ?? "?"}.{system.Method.Name}");
        return this;
    }

    /// <summary>Registers a fully configured <see cref="SystemDescriptor"/> to a stage.</summary>
    /// <param name="stage">The <see cref="Stage"/> during which the system should execute.</param>
    /// <param name="descriptor">A pre-configured system descriptor with name, conditions, and resource access metadata.</param>
    /// <returns>This <see cref="App"/> instance for fluent chaining.</returns>
    /// <seealso cref="SystemDescriptor"/>
    public App AddSystem(Stage stage, SystemDescriptor descriptor)
    {
        Schedule.AddSystem(stage, descriptor);
        Logger.Trace($"System descriptor registered to stage {stage}: {descriptor.Name}");
        return this;
    }

    // ── Resource helpers ───────────────────────────────────────────────

    /// <summary>Inserts or replaces a world resource of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The resource type. Each type may have at most one instance in the <see cref="World"/>.</typeparam>
    /// <param name="value">The resource instance to store.</param>
    /// <returns>This <see cref="App"/> instance for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// app.InsertResource(new GameState { Level = 1, IsPlaying = true });
    /// </code>
    /// </example>
    public App InsertResource<T>(T value) where T : notnull
    {
        World.InsertResource(value);
        Logger.Trace($"Resource inserted: {typeof(T).Name}");
        return this;
    }

    /// <summary>
    /// Returns the existing resource of type <typeparamref name="T"/>, or inserts <paramref name="value"/> and returns it.
    /// Atomic - safe for concurrent callers.
    /// </summary>
    /// <typeparam name="T">The resource type to retrieve or insert.</typeparam>
    /// <param name="value">The fallback value to insert if the resource does not exist.</param>
    /// <returns>The existing or newly inserted resource instance.</returns>
    public T GetOrInsertResource<T>(T value) where T : notnull
        => World.GetOrInsertResource(value);

    /// <summary>
    /// Returns the existing resource of type <typeparamref name="T"/>, or creates a default instance via <c>new T()</c>,
    /// inserts it, and returns it.
    /// </summary>
    /// <typeparam name="T">The resource type. Must have a public parameterless constructor.</typeparam>
    /// <returns>The existing or newly created resource instance.</returns>
    public T InitResource<T>() where T : notnull, new()
        => World.InitResource<T>();

    // ── Execution ──────────────────────────────────────────────────────

    /// <summary>
    /// Runs the application: executes <see cref="Stage.Startup"/> once, enters the per-frame main loop,
    /// then runs <see cref="Stage.Cleanup"/> on exit.
    /// </summary>
    /// <remarks>
    /// <para>The main loop is driven by the <see cref="IMainLoopDriver"/> resource, which must be
    /// present in the <see cref="World"/> (typically inserted by a window plugin such as
    /// <c>AppWindowPlugin</c>).</para>
    /// <para>Each frame executes all stages from <see cref="Stage.First"/> through <see cref="Stage.Last"/>
    /// in fixed order. After the loop exits, <see cref="Stage.Cleanup"/> runs, the driver is shut down,
    /// and all <see cref="IDisposable"/> resources are disposed.</para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no <see cref="IMainLoopDriver"/> resource has been inserted into the <see cref="World"/>.
    /// </exception>
    /// <seealso cref="IMainLoopDriver"/>
    /// <seealso cref="Stage"/>
    public void Run()
    {
        Logger.Info("App.Run() - Resolving main loop driver...");
        var loop = World.Resource<IMainLoopDriver>();
        Logger.Info($"Main loop driver: {loop.GetType().Name}");

        Logger.Info("Running Startup stage - one-time initialization systems...");
        Schedule.RunStage(Stage.Startup, World);
        Logger.Info("Startup stage complete.");

        Logger.Info("Entering main loop - per-frame execution begins.");
        loop.Run(() =>
        {
            _frameCount++;
            if (_frameCount <= 3 || (_frameCount % 1000 == 0))
                Logger.FrameTrace($"Frame #{_frameCount} begin");

            foreach (var stage in StageOrder.FrameStages())
                Schedule.RunStage(stage, World);
        });

        Logger.Info($"Main loop exited after {_frameCount} frames.");
        Logger.Info("Running Cleanup stage - teardown and resource disposal...");
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

    /// <summary>
    /// Disposes the <see cref="World"/> and all its <see cref="IDisposable"/> resources.
    /// Safe to call multiple times; subsequent calls are no-ops.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        World.Dispose();
        Logger.Trace("App disposed.");
    }
}

