namespace Engine;

/// <summary>Fixed execution phases processed in a strict order each frame.</summary>
/// <example>
/// <code>
/// // Register a system to the Update stage
/// app.AddSystem(Stage.Update, static world =>
/// {
///     var ecs = world.Resource&lt;EcsWorld&gt;();
///     foreach (var (e, pos, vel) in ecs.Query&lt;Position, Velocity&gt;())
///         ecs.Update(e, new Position(pos.X + vel.X, pos.Y + vel.Y));
/// });
///
/// // One-time init in Startup, teardown in Cleanup
/// app.AddSystem(Stage.Startup, LoadAssets);
/// app.AddSystem(Stage.Cleanup, ReleaseGpuResources);
/// </code>
/// </example>
public enum Stage
{
    /// <summary>Runs once at application start before the main loop.</summary>
    Startup,
    /// <summary>First per-frame stage - time updates, input polling.</summary>
    First,
    /// <summary>Pre-update logic - physics preparation, AI sensing.</summary>
    PreUpdate,
    /// <summary>Main gameplay logic.</summary>
    Update,
    /// <summary>Post-update logic - constraint solving, transform propagation.</summary>
    PostUpdate,
    /// <summary>Rendering commands - draw calls, GPU submission.</summary>
    Render,
    /// <summary>Last per-frame stage - diagnostic flush, event cleanup.</summary>
    Last,
    /// <summary>Runs once after the main loop exits - teardown and resource disposal.</summary>
    Cleanup,
}

/// <summary>Provides ordered stage sequences for iteration.</summary>
public static class StageOrder
{
    private static readonly Stage[] All =
    [
        Stage.Startup,
        Stage.First,
        Stage.PreUpdate,
        Stage.Update,
        Stage.PostUpdate,
        Stage.Render,
        Stage.Last,
        Stage.Cleanup,
    ];

    private static readonly Stage[] Frame =
    [
        Stage.First,
        Stage.PreUpdate,
        Stage.Update,
        Stage.PostUpdate,
        Stage.Render,
        Stage.Last,
    ];

    /// <summary>All stages in execution order (Startup → … → Cleanup).</summary>
    public static ReadOnlySpan<Stage> AllInOrder() => All;

    /// <summary>Per-frame stages only (First → … → Last), excluding Startup and Cleanup.</summary>
    public static ReadOnlySpan<Stage> FrameStages() => Frame;
}
