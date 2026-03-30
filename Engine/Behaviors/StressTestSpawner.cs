namespace Engine;

/// <summary>Spawns a batch of entities with <see cref="EntityCounter"/> when Space is pressed.</summary>
[Behavior]
public struct StressTestSpawner
{
    private const int BatchSize = 100_000;
    private static readonly ILogger Logger = Log.Category("Engine.StressTest");

    /// <summary>Spawns a batch of entities on a single Space press (not held).</summary>
    [OnUpdate]
    public static void OnSpacePressed(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space))
            return;

        Logger.Info($"Spawning {BatchSize:N0} entities...");

        for (int i = 0; i < BatchSize; i++)
        {
            ctx.Cmd.Spawn((id, ecs) =>
            {
                ecs.Add(id, new EntityCounter());
            });
        }

        Logger.Info($"Queued {BatchSize:N0} entity spawns (will apply after PostUpdate).");
    }
}

