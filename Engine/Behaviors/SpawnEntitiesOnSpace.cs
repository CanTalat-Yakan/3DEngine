namespace Engine;

/// <summary>Spawns 100k entities with CounterComponent when Space is pressed (once per second).</summary>
[Behavior]
public struct SpawnEntitiesOnSpace
{
    private static bool _spawned;

    [OnUpdate]
    public static void Update(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyDown(Key.Space) || _spawned)
            return;

        _spawned = true;

        for (int i = 0; i < 100_000; i++)
        {
            var e = ctx.Ecs.Spawn();
            ctx.Ecs.Add(e, new CounterComponent());
        }
    }

    [OnPostUpdate]
    public static void LateUpdate(BehaviorContext ctx)
    {
        if (ctx.Time.ElapsedSeconds % 1.0 < ctx.Time.DeltaSeconds)
            _spawned = false;
    }
}

