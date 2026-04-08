using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Engine;

/// <summary>
/// System delegate: receives the <see cref="World"/> to read/write resources and entities.
/// This is the fundamental unit of work in the ECS schedule.
/// </summary>
/// <param name="world">The shared <see cref="World"/> containing all resources and entity data.</param>
/// <seealso cref="SystemDescriptor"/>
/// <seealso cref="Schedule"/>
public delegate void SystemFn(World world);

/// <summary>Declares whether a system may run on worker threads or must run on the main thread.</summary>
/// <remarks>
/// Systems that interact with platform APIs (e.g., SDL window, GPU context) should use
/// <see cref="MainThread"/> to prevent cross-thread access violations.
/// </remarks>
public enum ThreadAffinity
{
    /// <summary>The system may run on any thread, including worker threads in parallel batches.</summary>
    Any,
    /// <summary>The system must execute on the main thread. It forms its own sequential batch.</summary>
    MainThread,
}

/// <summary>
/// Wraps a <see cref="SystemFn"/> with a human-readable name, optional run condition,
/// thread affinity, and resource access metadata for the parallel scheduler.
/// </summary>
/// <remarks>
/// <para>Used internally by <see cref="Schedule"/> and exposed via <see cref="ScheduleDiagnostics"/>.</para>
/// <para>
/// Resource access metadata (<see cref="Read{T}"/>/<see cref="Write{T}"/>) enables the scheduler to
/// build parallel execution batches. Systems without explicit metadata are treated conservatively
/// and serialized to prevent data races.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var desc = new SystemDescriptor(MySystem, "Physics.Integrate")
///     .Read&lt;Time&gt;()
///     .Write&lt;EcsWorld&gt;()
///     .RunIf(world => world.Resource&lt;GameState&gt;().IsPlaying);
///
/// app.AddSystem(Stage.Update, desc);
/// </code>
/// </example>
/// <seealso cref="Schedule"/>
/// <seealso cref="SystemFn"/>
public sealed class SystemDescriptor
{
    /// <summary>Human-readable label inferred from the delegate or set explicitly.</summary>
    public string Name { get; }

    /// <summary>The system delegate to invoke when this descriptor is executed.</summary>
    public SystemFn System { get; }

    /// <summary>Optional predicate - when set, the system only runs if this returns <c>true</c>.</summary>
    public Func<World, bool>? RunCondition { get => _runCondition; init => _runCondition = value; }
    private Func<World, bool>? _runCondition;

    /// <summary>Thread affinity for this system. Defaults to <see cref="ThreadAffinity.Any"/>.</summary>
    public ThreadAffinity Affinity { get; private set; } = ThreadAffinity.Any;

    /// <summary>Resource types this system declares as read-only dependencies.</summary>
    public IReadOnlyCollection<Type> Reads => _reads;

    /// <summary>Resource types this system declares as read-write dependencies.</summary>
    public IReadOnlyCollection<Type> Writes => _writes;

    /// <summary>
    /// <c>true</c> when this system declares at least one explicit resource read/write dependency.
    /// Systems without explicit access are treated as broad writers by the parallel scheduler.
    /// </summary>
    public bool HasExplicitAccess => _reads.Count > 0 || _writes.Count > 0;

    private readonly HashSet<Type> _reads = [];
    private readonly HashSet<Type> _writes = [];

    /// <summary>Creates a new system descriptor wrapping the specified delegate.</summary>
    /// <param name="system">The system delegate to execute.</param>
    /// <param name="name">
    /// Optional human-readable name. When <c>null</c>, inferred from the delegate's
    /// declaring type and method name (e.g., <c>"MyBehavior.Update"</c>).
    /// </param>
    public SystemDescriptor(SystemFn system, string? name = null)
    {
        System = system;
        Name = name ?? InferName(system);
    }

    /// <summary>Infers a human-readable name from the delegate's method metadata.</summary>
    /// <param name="system">The system delegate to inspect.</param>
    /// <returns>A string in the format <c>"DeclaringType.MethodName"</c>.</returns>
    private static string InferName(SystemFn system)
    {
        var method = system.Method;
        var type = method.DeclaringType?.Name ?? "?";
        return $"{type}.{method.Name}";
    }

    /// <summary>Marks this system as main-thread-only, preventing it from running in parallel batches.</summary>
    /// <returns>This descriptor for fluent chaining.</returns>
    /// <seealso cref="ThreadAffinity.MainThread"/>
    public SystemDescriptor MainThreadOnly()
    {
        Affinity = ThreadAffinity.MainThread;
        return this;
    }

    /// <summary>
    /// Attaches a Bevy-style <c>run_if</c> condition. The system is skipped for the current frame
    /// when <paramref name="condition"/> returns <c>false</c>.
    /// </summary>
    /// <param name="condition">A predicate evaluated against the <see cref="World"/> each frame.</param>
    /// <returns>This descriptor for fluent chaining.</returns>
    public SystemDescriptor RunIf(Func<World, bool> condition)
    {
        _runCondition = condition;
        return this;
    }

    /// <summary>Declares a read-only dependency on a resource type for the parallel scheduler.</summary>
    /// <typeparam name="T">The resource type this system reads.</typeparam>
    /// <returns>This descriptor for fluent chaining.</returns>
    /// <remarks>Multiple readers of the same resource can run in parallel.</remarks>
    public SystemDescriptor Read<T>() where T : notnull
    {
        _reads.Add(typeof(T));
        return this;
    }

    /// <summary>Declares a read-write dependency on a resource type for the parallel scheduler.</summary>
    /// <typeparam name="T">The resource type this system writes.</typeparam>
    /// <returns>This descriptor for fluent chaining.</returns>
    /// <remarks>A writer conflicts with all other readers and writers of the same resource.</remarks>
    public SystemDescriptor Write<T>() where T : notnull
    {
        _writes.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Determines whether this system conflicts with <paramref name="other"/> based on
    /// declared resource access. Used by the scheduler to build safe parallel batches.
    /// </summary>
    /// <param name="other">The other system descriptor to check against.</param>
    /// <returns><c>true</c> if the systems cannot safely run in parallel; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Conservative mode: systems without explicit access metadata are treated as broad writers,
    /// so they conflict with everything to prevent data races.
    /// </remarks>
    internal bool ConflictsWith(SystemDescriptor other)
    {
        // Conservative mode: systems without explicit access metadata are treated as
        // broad writers so they don't race with annotated systems.
        if ((_reads.Count == 0 && _writes.Count == 0) || (other._reads.Count == 0 && other._writes.Count == 0))
            return true;

        if (_writes.Count > 0)
        {
            foreach (var t in _writes)
                if (other._writes.Contains(t) || other._reads.Contains(t))
                    return true;
        }

        if (_reads.Count > 0)
        {
            foreach (var t in _reads)
                if (other._writes.Contains(t))
                    return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to determine the specific conflict reason between this system and <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The other system descriptor to check against.</param>
    /// <param name="reason">When this method returns <c>true</c>, contains a human-readable conflict description.</param>
    /// <returns><c>true</c> if a conflict exists; otherwise <c>false</c>.</returns>
    internal bool TryGetConflictReason(SystemDescriptor other, out string reason)
    {
        if (!HasExplicitAccess || !other.HasExplicitAccess)
        {
            reason = "missing explicit access metadata";
            return true;
        }

        foreach (var t in _writes)
        {
            if (other._writes.Contains(t))
            {
                reason = $"write/write {t.Name}";
                return true;
            }
            if (other._reads.Contains(t))
            {
                reason = $"write/read {t.Name}";
                return true;
            }
        }

        foreach (var t in _reads)
        {
            if (other._writes.Contains(t))
            {
                reason = $"read/write {t.Name}";
                return true;
            }
        }

        reason = string.Empty;
        return false;
    }
}

/// <summary>
/// Schedules systems into Bevy-like stages and executes them.
/// Stages run sequentially in a fixed order; within a stage, systems run in parallel by default.
/// </summary>
/// <remarks>
/// <para>
/// Features: named system descriptors, optional <c>run_if</c>-style conditions,
/// per-system exception isolation, removal/introspection APIs, and frame-level
/// <see cref="ScheduleDiagnostics"/>.
/// </para>
/// <para>
/// <b>Parallelism model:</b> Each stage's systems are partitioned into execution batches.
/// Systems that share no conflicting resource access (based on <see cref="SystemDescriptor.Read{T}"/>
/// and <see cref="SystemDescriptor.Write{T}"/> metadata) run in parallel within a batch.
/// Systems lacking metadata are conservatively serialized.
/// </para>
/// <para>
/// <b>Default threading:</b> <see cref="Stage.Startup"/>, <see cref="Stage.Render"/>, and
/// <see cref="Stage.Cleanup"/> are single-threaded by default. All other stages are parallel.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var schedule = new Schedule();
/// schedule.AddSystem(Stage.Update, static world =>
/// {
///     var time = world.Resource&lt;Time&gt;();
///     Console.WriteLine($"Frame delta: {time.DeltaSeconds:F4}s");
/// });
/// schedule.AddSystem(Stage.Update, PhysicsStep, world => world.ContainsResource&lt;PhysicsWorld&gt;());
/// schedule.RunStage(Stage.Update, world);
/// </code>
/// </example>
/// <seealso cref="Stage"/>
/// <seealso cref="StageOrder"/>
/// <seealso cref="SystemDescriptor"/>
/// <seealso cref="ScheduleDiagnostics"/>
public sealed class Schedule
{
    private static readonly ILogger Logger = Log.Category("Engine.Schedule");

    private readonly Lock _lock = new();
    private readonly Dictionary<Stage, List<SystemDescriptor>> _systemsByStage = new();
    private readonly HashSet<Stage> _parallelStages = [];
    private readonly HashSet<string> _missingAccessWarnings = [];

    /// <summary>Per-stage and per-system timing recorded during execution.</summary>
    /// <seealso cref="ScheduleDiagnostics"/>
    public ScheduleDiagnostics Diagnostics { get; } = new();

    /// <summary>
    /// Initializes a new <see cref="Schedule"/> with all stages pre-registered.
    /// <see cref="Stage.Startup"/>, <see cref="Stage.Render"/>, and <see cref="Stage.Cleanup"/>
    /// are configured as single-threaded; all other stages default to parallel.
    /// </summary>
    public Schedule()
    {
        foreach (var stage in StageOrder.AllInOrder())
        {
            _systemsByStage[stage] = [];
            _parallelStages.Add(stage);
        }
        SetSingleThreaded(Stage.Startup);
        SetSingleThreaded(Stage.Render);
        SetSingleThreaded(Stage.Cleanup);
        Logger.Trace("Schedule created - all stages initialized, Startup, Render, and Cleanup stages set to single-threaded.");
    }

    // ── Registration ───────────────────────────────────────────────────

    /// <summary>Adds a system delegate to the specified execution stage.</summary>
    /// <param name="stage">The <see cref="Stage"/> to register the system in.</param>
    /// <param name="system">The system delegate to execute when the stage runs.</param>
    /// <returns>This <see cref="Schedule"/> instance for fluent chaining.</returns>
    public Schedule AddSystem(Stage stage, SystemFn system)
    {
        lock (_lock)
            _systemsByStage[stage].Add(new SystemDescriptor(system));
        return this;
    }

    /// <summary>Adds a system with a Bevy-style <c>run_if</c> condition to the specified stage.</summary>
    /// <param name="stage">The <see cref="Stage"/> to register the system in.</param>
    /// <param name="system">The system delegate to execute when the stage runs.</param>
    /// <param name="runCondition">A predicate evaluated each frame; the system is skipped when <c>false</c>.</param>
    /// <returns>This <see cref="Schedule"/> instance for fluent chaining.</returns>
    public Schedule AddSystem(Stage stage, SystemFn system, Func<World, bool> runCondition)
    {
        lock (_lock)
            _systemsByStage[stage].Add(new SystemDescriptor(system) { RunCondition = runCondition });
        return this;
    }

    /// <summary>Adds a fully configured <see cref="SystemDescriptor"/> to the specified stage.</summary>
    /// <param name="stage">The <see cref="Stage"/> to register the descriptor in.</param>
    /// <param name="descriptor">A pre-configured system descriptor with name, conditions, and access metadata.</param>
    /// <returns>This <see cref="Schedule"/> instance for fluent chaining.</returns>
    public Schedule AddSystem(Stage stage, SystemDescriptor descriptor)
    {
        lock (_lock)
            _systemsByStage[stage].Add(descriptor);
        return this;
    }

    /// <summary>Removes all systems matching <paramref name="predicate"/> from the specified stage.</summary>
    /// <param name="stage">The <see cref="Stage"/> to remove systems from.</param>
    /// <param name="predicate">A predicate selecting which systems to remove.</param>
    /// <returns>The number of systems removed.</returns>
    public int RemoveSystems(Stage stage, Predicate<SystemDescriptor> predicate)
    {
        lock (_lock)
            return _systemsByStage[stage].RemoveAll(predicate);
    }

    // ── Introspection ──────────────────────────────────────────────────

    /// <summary>Returns the number of systems registered to the given stage.</summary>
    /// <param name="stage">The <see cref="Stage"/> to query.</param>
    /// <returns>The count of systems in the specified stage.</returns>
    public int SystemCount(Stage stage)
    {
        lock (_lock)
            return _systemsByStage[stage].Count;
    }

    /// <summary>Total number of systems across all stages.</summary>
    public int TotalSystemCount
    {
        get
        {
            lock (_lock)
            {
                int count = 0;
                foreach (var kv in _systemsByStage)
                    count += kv.Value.Count;
                return count;
            }
        }
    }

    // ── Parallelism control ────────────────────────────────────────────

    /// <summary>Marks a stage for parallel execution (default). Pass <c>false</c> to run single-threaded.</summary>
    /// <param name="stage">The <see cref="Stage"/> to configure.</param>
    /// <param name="parallel"><c>true</c> (default) for parallel execution; <c>false</c> for sequential.</param>
    /// <returns>This <see cref="Schedule"/> instance for fluent chaining.</returns>
    public Schedule SetParallel(Stage stage, bool parallel = true)
    {
        lock (_lock)
        {
            if (parallel) _parallelStages.Add(stage); else _parallelStages.Remove(stage);
        }
        return this;
    }

    /// <summary>Marks a stage to run systems sequentially. Pass <c>false</c> to restore parallel execution.</summary>
    /// <param name="stage">The <see cref="Stage"/> to configure.</param>
    /// <param name="singleThreaded"><c>true</c> (default) for sequential execution; <c>false</c> for parallel.</param>
    /// <returns>This <see cref="Schedule"/> instance for fluent chaining.</returns>
    public Schedule SetSingleThreaded(Stage stage, bool singleThreaded = true)
        => SetParallel(stage, !singleThreaded);

    // ── Execution ──────────────────────────────────────────────────────

    /// <summary>
    /// Runs all systems registered to the specified stage, using parallel execution if enabled.
    /// Each system is isolated - exceptions are caught, logged, and do not prevent subsequent systems from running.
    /// </summary>
    /// <param name="stage">The <see cref="Stage"/> to execute.</param>
    /// <param name="world">The <see cref="World"/> passed to each system delegate.</param>
    public void RunStage(Stage stage, World world)
    {
        List<SystemDescriptor> list;
        bool isParallel;
        bool stageMarkedParallel;

        lock (_lock)
        {
            list = _systemsByStage[stage];
            if (list.Count == 0) return;
            stageMarkedParallel = _parallelStages.Contains(stage);
            isParallel = stageMarkedParallel && list.Count > 1;
        }

        Logger.FrameTrace($"Stage {stage}: executing {list.Count} system(s) [{(isParallel ? "parallel" : "sequential")}]");

        var stageSw = Stopwatch.StartNew();

        if (isParallel)
            RunParallel(stage, list, world);
        else
            RunSequential(stage, list, world, stageMarkedParallel);

        stageSw.Stop();
        Diagnostics.RecordStage(stage, stageSw.Elapsed);
        Logger.FrameTrace($"Stage {stage}: completed in {stageSw.Elapsed.TotalMilliseconds:F2}ms");
    }

    /// <summary>Runs all stages in their fixed order (Startup → … → Cleanup).</summary>
    /// <param name="world">The <see cref="World"/> passed to each system delegate.</param>
    public void Run(World world)
    {
        foreach (var stage in StageOrder.AllInOrder())
            RunStage(stage, world);
    }

    // ── Private helpers ────────────────────────────────────────────────

    /// <summary>Executes systems sequentially within a single stage, recording diagnostics for each.</summary>
    /// <param name="stage">The stage being executed.</param>
    /// <param name="systems">The list of system descriptors to run.</param>
    /// <param name="world">The shared world instance.</param>
    /// <param name="stageMarkedParallel">Whether the stage was originally marked for parallel execution.</param>
    private void RunSequential(Stage stage, List<SystemDescriptor> systems, World world, bool stageMarkedParallel)
    {
        var sequentialReason = stageMarkedParallel
            ? (systems.Count <= 1 ? "single-system-stage" : "serialized-by-conflicts")
            : "stage-configured-single-threaded";

        var batches = systems.Select(s => new List<SystemDescriptor> { s }).ToList();
        var notes = batches.Select(_ => (IReadOnlyList<string>)new[] { sequentialReason }).ToList();
        Diagnostics.RecordBatches(stage, batches);
        Diagnostics.RecordBatchNotes(stage, notes);

        var span = CollectionsMarshal.AsSpan(systems);
        for (int i = 0; i < span.Length; i++)
        {
            ref var desc = ref span[i];

            if (desc.RunCondition is { } cond && !cond(world))
            {
                Logger.FrameTrace($"  ⏭ {desc.Name} - skipped (run condition false)");
                continue;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                desc.System(world);
            }
            catch (Exception ex)
            {
                Logger.Error($"System '{desc.Name}' threw in stage {stage}", ex);
            }
            sw.Stop();
            Diagnostics.RecordSystem(stage, desc.Name, sw.Elapsed);
        }
    }

    /// <summary>
    /// Builds execution batches from resource access metadata and runs them in parallel.
    /// Main-thread systems form their own single-item batch.
    /// </summary>
    /// <param name="stage">The stage being executed.</param>
    /// <param name="systems">The list of system descriptors to partition and run.</param>
    /// <param name="world">The shared world instance.</param>
    private void RunParallel(Stage stage, List<SystemDescriptor> systems, World world)
    {
        WarnForMissingAccessMetadata(stage, systems);

        // Build execution batches where systems can safely run together.
        // Main-thread systems form their own single-item batch and flush pending parallel work.
        var batches = BuildExecutionBatches(systems, out var notes);
        Diagnostics.RecordBatches(stage, batches);
        Diagnostics.RecordBatchNotes(stage, notes);

        for (int i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            var mode = batch.Count == 1 || batch[0].Affinity == ThreadAffinity.MainThread ? "sequential" : "parallel";
            Logger.FrameTrace($"  batch {i + 1}/{batches.Count} [{mode}] => {string.Join(", ", batch.Select(d => d.Name))}");
        }

        foreach (var batch in batches)
        {
            if (batch.Count == 1)
            {
                ExecuteSystem(stage, batch[0], world);
                continue;
            }

            Parallel.ForEach(batch, desc => ExecuteSystem(stage, desc, world));
        }
    }

    /// <summary>
    /// Partitions systems into execution batches where systems within a batch have no
    /// conflicting resource access and can safely run in parallel.
    /// </summary>
    /// <param name="systems">The systems to partition.</param>
    /// <param name="notes">
    /// When this method returns, contains per-batch notes describing conflict reasons
    /// or placement markers (e.g., <c>"main-thread-only"</c>).
    /// </param>
    /// <returns>A list of batches, each containing non-conflicting system descriptors.</returns>
    private static List<List<SystemDescriptor>> BuildExecutionBatches(List<SystemDescriptor> systems, out List<IReadOnlyList<string>> notes)
    {
        var batches = new List<List<SystemDescriptor>>();
        var batchNotes = new List<List<string>>();

        foreach (var desc in systems)
        {
            if (desc.Affinity == ThreadAffinity.MainThread)
            {
                batches.Add([desc]);
                batchNotes.Add(["main-thread-only"]);
                continue;
            }

            var placed = false;
            for (int i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                if (batch.Count == 1 && batch[0].Affinity == ThreadAffinity.MainThread)
                    continue;

                var conflict = false;
                for (int j = 0; j < batch.Count; j++)
                {
                    if (desc.TryGetConflictReason(batch[j], out var reason))
                    {
                        conflict = true;
                        if (!batchNotes[i].Contains(reason))
                            batchNotes[i].Add(reason);
                        break;
                    }
                }

                if (conflict)
                    continue;

                batch.Add(desc);
                placed = true;
                break;
            }

            if (!placed)
            {
                batches.Add([desc]);
                batchNotes.Add([]);
            }
        }

        notes = batchNotes.Select(n => (IReadOnlyList<string>)n.ToArray()).ToList();
        return batches;
    }

    /// <summary>
    /// Emits a one-time warning for systems that lack explicit <see cref="SystemDescriptor.Read{T}"/>/<see cref="SystemDescriptor.Write{T}"/>
    /// metadata in a parallel stage. Such systems are conservatively serialized.
    /// </summary>
    /// <param name="stage">The stage being checked.</param>
    /// <param name="systems">The systems to inspect.</param>
    private void WarnForMissingAccessMetadata(Stage stage, List<SystemDescriptor> systems)
    {
        for (int i = 0; i < systems.Count; i++)
        {
            var desc = systems[i];
            if (desc.HasExplicitAccess || desc.Affinity == ThreadAffinity.MainThread)
                continue;

            var key = $"{stage}:{desc.Name}";
            lock (_lock)
            {
                if (!_missingAccessWarnings.Add(key))
                    continue;
            }

            Logger.Warn($"System '{desc.Name}' in stage {stage} has no Read/Write metadata; scheduler is using conservative conflict mode.");
        }
    }

    /// <summary>
    /// Executes a single system with run-condition checking, timing, and exception isolation.
    /// </summary>
    /// <param name="stage">The stage context for logging.</param>
    /// <param name="desc">The system descriptor to execute.</param>
    /// <param name="world">The shared world instance.</param>
    private void ExecuteSystem(Stage stage, SystemDescriptor desc, World world)
    {
        if (desc.RunCondition is { } cond && !cond(world))
        {
            Logger.FrameTrace($"  ⏭ {desc.Name} - skipped (run condition false)");
            return;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            desc.System(world);
        }
        catch (Exception ex)
        {
            Logger.Error($"System '{desc.Name}' threw in stage {stage}", ex);
        }
        sw.Stop();
        Diagnostics.RecordSystem(stage, desc.Name, sw.Elapsed);
    }
}

/// <summary>
/// Tracks per-stage and per-system execution timing for the most recent frame.
/// Populated by <see cref="Schedule"/> during execution and queryable for diagnostics and debugging HUDs.
/// </summary>
/// <remarks>
/// All public accessors return snapshot copies to avoid data races with the schedule's recording thread.
/// This class is thread-safe for concurrent reads and writes.
/// </remarks>
/// <seealso cref="Schedule"/>
public sealed class ScheduleDiagnostics
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Stage, TimeSpan> _stageTimes = new();
    private readonly Dictionary<(Stage Stage, string System), TimeSpan> _systemTimes = new();
    private readonly Dictionary<Stage, List<IReadOnlyList<string>>> _stageBatches = new();
    private readonly Dictionary<Stage, List<IReadOnlyList<string>>> _stageBatchNotes = new();

    /// <summary>Records the total duration of a stage execution.</summary>
    /// <param name="stage">The stage that was executed.</param>
    /// <param name="elapsed">The wall-clock duration of the stage.</param>
    internal void RecordStage(Stage stage, TimeSpan elapsed)
    {
        lock (_lock) _stageTimes[stage] = elapsed;
    }

    /// <summary>Records the duration of a single system execution.</summary>
    /// <param name="stage">The stage containing the system.</param>
    /// <param name="systemName">The human-readable system name.</param>
    /// <param name="elapsed">The wall-clock duration of the system.</param>
    internal void RecordSystem(Stage stage, string systemName, TimeSpan elapsed)
    {
        lock (_lock) _systemTimes[(stage, systemName)] = elapsed;
    }

    /// <summary>Records the scheduler batch plan used for a parallel stage execution.</summary>
    /// <param name="stage">The stage whose batches are being recorded.</param>
    /// <param name="batches">The list of execution batches, each containing system descriptors.</param>
    internal void RecordBatches(Stage stage, List<List<SystemDescriptor>> batches)
    {
        lock (_lock)
        {
            _stageBatches[stage] = batches
                .Select(batch => (IReadOnlyList<string>)batch.Select(desc => desc.Name).ToArray())
                .ToList();
        }
    }

    /// <summary>Records conflict/placement notes for each computed batch.</summary>
    /// <param name="stage">The stage whose batch notes are being recorded.</param>
    /// <param name="notes">Per-batch notes describing conflict reasons or placement markers.</param>
    internal void RecordBatchNotes(Stage stage, List<IReadOnlyList<string>> notes)
    {
        lock (_lock)
            _stageBatchNotes[stage] = notes.ToList();
    }

    /// <summary>Returns the last recorded duration for the specified stage.</summary>
    /// <param name="stage">The stage to query.</param>
    /// <returns>The duration of the last execution, or <see cref="TimeSpan.Zero"/> if never recorded.</returns>
    public TimeSpan GetStageDuration(Stage stage)
    {
        lock (_lock) return _stageTimes.GetValueOrDefault(stage);
    }

    /// <summary>Returns the last recorded duration for a named system in a stage.</summary>
    /// <param name="stage">The stage containing the system.</param>
    /// <param name="systemName">The human-readable name of the system.</param>
    /// <returns>The duration of the last execution, or <see cref="TimeSpan.Zero"/> if never recorded.</returns>
    public TimeSpan GetSystemDuration(Stage stage, string systemName)
    {
        lock (_lock) return _systemTimes.GetValueOrDefault((stage, systemName));
    }

    /// <summary>Snapshot of all stage durations from the last frame.</summary>
    /// <returns>A dictionary mapping each executed <see cref="Stage"/> to its wall-clock duration.</returns>
    public IReadOnlyDictionary<Stage, TimeSpan> StageDurations
    {
        get { lock (_lock) return new Dictionary<Stage, TimeSpan>(_stageTimes); }
    }

    /// <summary>Snapshot of all per-system durations from the last frame.</summary>
    /// <returns>A dictionary mapping (stage, system name) pairs to their wall-clock durations.</returns>
    public IReadOnlyDictionary<(Stage Stage, string System), TimeSpan> SystemDurations
    {
        get { lock (_lock) return new Dictionary<(Stage, string), TimeSpan>(_systemTimes); }
    }

    /// <summary>Snapshot of stage batch composition from the most recent frame.</summary>
    /// <returns>A dictionary mapping each stage to its list of execution batches (each batch is a list of system names).</returns>
    public IReadOnlyDictionary<Stage, IReadOnlyList<IReadOnlyList<string>>> StageBatches
    {
        get
        {
            lock (_lock)
            {
                return _stageBatches.ToDictionary(
                    kv => kv.Key,
                    kv => (IReadOnlyList<IReadOnlyList<string>>)kv.Value.Select(batch => (IReadOnlyList<string>)batch.ToArray()).ToArray());
            }
        }
    }

    /// <summary>Snapshot of scheduler batch notes (conflicts/main-thread markers) from the most recent frame.</summary>
    /// <returns>A dictionary mapping each stage to per-batch note lists explaining scheduling decisions.</returns>
    public IReadOnlyDictionary<Stage, IReadOnlyList<IReadOnlyList<string>>> StageBatchNotes
    {
        get
        {
            lock (_lock)
            {
                return _stageBatchNotes.ToDictionary(
                    kv => kv.Key,
                    kv => (IReadOnlyList<IReadOnlyList<string>>)kv.Value.Select(noteList => (IReadOnlyList<string>)noteList.ToArray()).ToArray());
            }
        }
    }

    /// <summary>Clears all recorded timings, batch compositions, and notes.</summary>
    public void Reset()
    {
        lock (_lock)
        {
            _stageTimes.Clear();
            _systemTimes.Clear();
            _stageBatches.Clear();
            _stageBatchNotes.Clear();
        }
    }
}
