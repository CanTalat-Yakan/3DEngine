using System.Collections.Generic;
using System.Threading.Tasks;

namespace Engine;

public delegate void SystemFn(World world);

/// <summary>
/// Schedules systems into Bevy-like stages and executes them in order.
/// Supports optional per-stage parallel execution (unsafe if systems mutate shared state without synchronization).
/// </summary>
public sealed class Schedule
{
    private readonly Dictionary<Stage, List<SystemFn>> _systemsByStage = new();
    private readonly HashSet<Stage> _parallelStages = new();

    public Schedule()
    {
        foreach (var stage in StageOrder.AllInOrder())
            _systemsByStage[stage] = new();
    }

    public Schedule AddSystem(Stage stage, SystemFn system)
    {
        _systemsByStage[stage].Add(system);
        return this;
    }

    public Schedule SetParallel(Stage stage, bool parallel = true)
    {
        if (parallel) _parallelStages.Add(stage); else _parallelStages.Remove(stage);
        return this;
    }

    public void RunStage(Stage stage, World world)
    {
        var list = _systemsByStage[stage];
        if (list.Count == 0) return;

        if (_parallelStages.Contains(stage) && list.Count > 1)
        {
            // naive parallel execution: systems are responsible for thread safety
            Parallel.ForEach(list, sys => sys(world));
        }
        else
        {
            for (int i = 0; i < list.Count; i++)
                list[i](world);
        }
    }

    public void Run(World world)
    {
        foreach (var stage in StageOrder.AllInOrder())
            RunStage(stage, world);
    }
}
