using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Engine;

/// <summary>System delegate: receives the <see cref="World"/> to read/write resources and entities.</summary>
public delegate void SystemFn(World world);

/// <summary>Declares whether a system may run on worker threads or must run on the main thread.</summary>
public enum ThreadAffinity
{
    Any,
    MainThread,
}

/// <summary>
/// Wraps a <see cref="SystemFn"/> with a human-readable name and an optional run condition.
/// Used internally by <see cref="Schedule"/> and exposed via diagnostics.
/// </summary>
public sealed class SystemDescriptor
{
    /// <summary>Human-readable label inferred from the delegate or set explicitly.</summary>
    public string Name { get; }

    /// <summary>The system delegate to invoke.</summary>
    public SystemFn System { get; }

    /// <summary>Optional predicate — when set, the system only runs if this returns <c>true</c>.</summary>
    public Func<World, bool>? RunCondition { get; init; }

    /// <summary>Thread affinity for this system. Defaults to <see cref="ThreadAffinity.Any"/>.</summary>
    public ThreadAffinity Affinity { get; private set; } = ThreadAffinity.Any;

    /// <summary>Resource types this system reads.</summary>
    public IReadOnlyCollection<Type> Reads => _reads;

    /// <summary>Resource types this system writes.</summary>
    public IReadOnlyCollection<Type> Writes => _writes;

    private readonly HashSet<Type> _reads = [];
    private readonly HashSet<Type> _writes = [];

    public SystemDescriptor(SystemFn system, string? name = null)
    {
        System = system;
        Name = name ?? InferName(system);
    }

    private static string InferName(SystemFn system)
    {
        var method = system.Method;
        var type = method.DeclaringType?.Name ?? "?";
        return $"{type}.{method.Name}";
    }

    /// <summary>Marks this system as main-thread-only.</summary>
    public SystemDescriptor MainThreadOnly()
    {
        Affinity = ThreadAffinity.MainThread;
        return this;
    }

    /// <summary>Declares a read dependency on a resource type.</summary>
    public SystemDescriptor Read<T>() where T : notnull
    {
        _reads.Add(typeof(T));
        return this;
    }

    /// <summary>Declares a write dependency on a resource type.</summary>
    public SystemDescriptor Write<T>() where T : notnull
    {
        _writes.Add(typeof(T));
        return this;
    }

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
}

/// <summary>
/// Schedules systems into Bevy-like stages and executes them.
/// Stages run sequentially in a fixed order; within a stage, systems run in parallel by default.
/// <para>
/// Features: named system descriptors, optional <c>run_if</c>-style conditions,
/// per-system exception isolation, removal/introspection APIs, and frame-level
/// <see cref="ScheduleDiagnostics"/>.
/// </para>
/// </summary>
public sealed class Schedule
{
    private static readonly ILogger Logger = Log.Category("Engine.Schedule");

    private readonly Lock _lock = new();
    private readonly Dictionary<Stage, List<SystemDescriptor>> _systemsByStage = new();
    private readonly HashSet<Stage> _parallelStages = [];

    /// <summary>Per-stage and per-system timing recorded during execution.</summary>
    public ScheduleDiagnostics Diagnostics { get; } = new();

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
        Logger.Trace("Schedule created — all stages initialized, Startup, Render, and Cleanup stages set to single-threaded.");
    }

    // ── Registration ───────────────────────────────────────────────────

    /// <summary>Adds a system to the specified stage.</summary>
    public Schedule AddSystem(Stage stage, SystemFn system)
    {
        lock (_lock)
            _systemsByStage[stage].Add(new SystemDescriptor(system));
        return this;
    }

    /// <summary>Adds a system with a Bevy-style <c>run_if</c> condition to the specified stage.</summary>
    public Schedule AddSystem(Stage stage, SystemFn system, Func<World, bool> runCondition)
    {
        lock (_lock)
            _systemsByStage[stage].Add(new SystemDescriptor(system) { RunCondition = runCondition });
        return this;
    }

    /// <summary>Adds a fully configured <see cref="SystemDescriptor"/> to the specified stage.</summary>
    public Schedule AddSystem(Stage stage, SystemDescriptor descriptor)
    {
        lock (_lock)
            _systemsByStage[stage].Add(descriptor);
        return this;
    }

    /// <summary>Removes all systems matching <paramref name="predicate"/> from a stage. Returns the number removed.</summary>
    public int RemoveSystems(Stage stage, Predicate<SystemDescriptor> predicate)
    {
        lock (_lock)
            return _systemsByStage[stage].RemoveAll(predicate);
    }

    // ── Introspection ──────────────────────────────────────────────────

    /// <summary>Number of systems registered to the given stage.</summary>
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
    public Schedule SetParallel(Stage stage, bool parallel = true)
    {
        lock (_lock)
        {
            if (parallel) _parallelStages.Add(stage); else _parallelStages.Remove(stage);
        }
        return this;
    }

    /// <summary>Marks a stage to run systems sequentially. Pass <c>false</c> to restore parallel execution.</summary>
    public Schedule SetSingleThreaded(Stage stage, bool singleThreaded = true)
        => SetParallel(stage, !singleThreaded);

    // ── Execution ──────────────────────────────────────────────────────

    /// <summary>Runs all systems registered to a stage (parallel if enabled), with per-system exception isolation.</summary>
    public void RunStage(Stage stage, World world)
    {
        List<SystemDescriptor> list;
        bool isParallel;

        lock (_lock)
        {
            list = _systemsByStage[stage];
            if (list.Count == 0) return;
            isParallel = _parallelStages.Contains(stage) && list.Count > 1;
        }

        Logger.FrameTrace($"Stage {stage}: executing {list.Count} system(s) [{(isParallel ? "parallel" : "sequential")}]");

        var stageSw = Stopwatch.StartNew();

        if (isParallel)
            RunParallel(stage, list, world);
        else
            RunSequential(stage, list, world);

        stageSw.Stop();
        Diagnostics.RecordStage(stage, stageSw.Elapsed);
        Logger.FrameTrace($"Stage {stage}: completed in {stageSw.Elapsed.TotalMilliseconds:F2}ms");
    }

    /// <summary>Runs all stages in fixed order.</summary>
    public void Run(World world)
    {
        foreach (var stage in StageOrder.AllInOrder())
            RunStage(stage, world);
    }

    // ── Private helpers ────────────────────────────────────────────────

    private void RunSequential(Stage stage, List<SystemDescriptor> systems, World world)
    {
        var span = CollectionsMarshal.AsSpan(systems);
        for (int i = 0; i < span.Length; i++)
        {
            ref var desc = ref span[i];

            if (desc.RunCondition is { } cond && !cond(world))
            {
                Logger.FrameTrace($"  ⏭ {desc.Name} — skipped (run condition false)");
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

    private void RunParallel(Stage stage, List<SystemDescriptor> systems, World world)
    {
        // Build execution batches where systems can safely run together.
        // Main-thread systems form their own single-item batch and flush pending parallel work.
        var batches = BuildExecutionBatches(systems);
        Diagnostics.RecordBatches(stage, batches);

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

    private static List<List<SystemDescriptor>> BuildExecutionBatches(List<SystemDescriptor> systems)
    {
        var batches = new List<List<SystemDescriptor>>();

        foreach (var desc in systems)
        {
            if (desc.Affinity == ThreadAffinity.MainThread)
            {
                batches.Add([desc]);
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
                    if (desc.ConflictsWith(batch[j]))
                    {
                        conflict = true;
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
                batches.Add([desc]);
        }

        return batches;
    }

    private void ExecuteSystem(Stage stage, SystemDescriptor desc, World world)
    {
        if (desc.RunCondition is { } cond && !cond(world))
        {
            Logger.FrameTrace($"  ⏭ {desc.Name} — skipped (run condition false)");
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

/// <summary>Tracks per-stage and per-system timing for the most recent frame.</summary>
public sealed class ScheduleDiagnostics
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Stage, TimeSpan> _stageTimes = new();
    private readonly Dictionary<(Stage Stage, string System), TimeSpan> _systemTimes = new();
    private readonly Dictionary<Stage, List<IReadOnlyList<string>>> _stageBatches = new();

    /// <summary>Records the total duration of a stage execution.</summary>
    internal void RecordStage(Stage stage, TimeSpan elapsed)
    {
        lock (_lock) _stageTimes[stage] = elapsed;
    }

    /// <summary>Records the duration of a single system execution.</summary>
    internal void RecordSystem(Stage stage, string systemName, TimeSpan elapsed)
    {
        lock (_lock) _systemTimes[(stage, systemName)] = elapsed;
    }

    /// <summary>Records the scheduler batch plan used for a parallel stage execution.</summary>
    internal void RecordBatches(Stage stage, List<List<SystemDescriptor>> batches)
    {
        lock (_lock)
        {
            _stageBatches[stage] = batches
                .Select(batch => (IReadOnlyList<string>)batch.Select(desc => desc.Name).ToArray())
                .ToList();
        }
    }

    /// <summary>Last recorded duration for a stage, or <see cref="TimeSpan.Zero"/>.</summary>
    public TimeSpan GetStageDuration(Stage stage)
    {
        lock (_lock) return _stageTimes.GetValueOrDefault(stage);
    }

    /// <summary>Last recorded duration for a named system in a stage, or <see cref="TimeSpan.Zero"/>.</summary>
    public TimeSpan GetSystemDuration(Stage stage, string systemName)
    {
        lock (_lock) return _systemTimes.GetValueOrDefault((stage, systemName));
    }

    /// <summary>Snapshot of all stage durations from the last frame.</summary>
    public IReadOnlyDictionary<Stage, TimeSpan> StageDurations
    {
        get { lock (_lock) return new Dictionary<Stage, TimeSpan>(_stageTimes); }
    }

    /// <summary>Snapshot of all per-system durations from the last frame.</summary>
    public IReadOnlyDictionary<(Stage Stage, string System), TimeSpan> SystemDurations
    {
        get { lock (_lock) return new Dictionary<(Stage, string), TimeSpan>(_systemTimes); }
    }

    /// <summary>Snapshot of stage batch composition from the most recent frame.</summary>
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

    /// <summary>Clears all recorded timings.</summary>
    public void Reset()
    {
        lock (_lock)
        {
            _stageTimes.Clear();
            _systemTimes.Clear();
            _stageBatches.Clear();
        }
    }
}
