namespace Engine;

public delegate void SystemFn(World world);

/// <summary>Schedules systems into Bevy-like stages and executes them (optionally in parallel).</summary>
public sealed class Schedule
{
    private readonly Dictionary<Stage, List<SystemFn>> _systemsByStage = new();
    private readonly HashSet<Stage> _parallelStages = new();

    public Schedule()
    {
        foreach (var stage in StageOrder.AllInOrder())
            _systemsByStage[stage] = new();
    }

    /// <summary>Adds a system to the specified stage.</summary>
    public Schedule AddSystem(Stage stage, SystemFn system)
    {
        _systemsByStage[stage].Add(system);
        return this;
    }

    /// <summary>Marks a stage systems list for parallel execution (naive Parallel.ForEach).</summary>
    public Schedule SetParallel(Stage stage, bool parallel = true)
    {
        if (parallel) _parallelStages.Add(stage); else _parallelStages.Remove(stage);
        return this;
    }

    /// <summary>Runs all systems registered to a stage (in parallel if enabled).</summary>
    public void RunStage(Stage stage, World world)
    {
        var list = _systemsByStage[stage];
        if (list.Count == 0) return;

        if (_parallelStages.Contains(stage) && list.Count > 1)
        {
            Parallel.ForEach(list, sys => sys(world));
        }
        else
        {
            for (int i = 0; i < list.Count; i++)
                list[i](world);
        }
    }

    /// <summary>Runs all stages in fixed order.</summary>
    public void Run(World world)
    {
        foreach (var stage in StageOrder.AllInOrder())
            RunStage(stage, world);
    }
}
