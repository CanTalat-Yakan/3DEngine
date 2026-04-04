using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Engine;

/// <summary>System delegate: receives the <see cref="World"/> to read/write resources and entities.</summary>
public delegate void SystemFn(World world);

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
        Parallel.ForEach(systems, desc =>
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
        });
    }
}

/// <summary>Tracks per-stage and per-system timing for the most recent frame.</summary>
public sealed class ScheduleDiagnostics
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Stage, TimeSpan> _stageTimes = new();
    private readonly Dictionary<(Stage Stage, string System), TimeSpan> _systemTimes = new();

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

    /// <summary>Clears all recorded timings.</summary>
    public void Reset()
    {
        lock (_lock)
        {
            _stageTimes.Clear();
            _systemTimes.Clear();
        }
    }
}
