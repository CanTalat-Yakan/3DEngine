using System.Collections.Generic;

namespace Engine;

/// <summary>
/// Bevy-like fixed stage labels to order systems.
/// </summary>
public enum Stage
{
    Startup,      // runs once before first frame
    First,        // very early per-frame systems
    PreUpdate,    // input, time, events
    Update,       // main gameplay logic
    PostUpdate,   // transform sync, housekeeping
    Render,       // rendering & UI submission
    Last,         // very late per-frame systems
}

public static class StageOrder
{
    private static readonly Stage[] Ordered = new Stage[]
    {
        Stage.Startup,
        Stage.First,
        Stage.PreUpdate,
        Stage.Update,
        Stage.PostUpdate,
        Stage.Render,
        Stage.Last,
    };

    public static IEnumerable<Stage> AllInOrder() => Ordered;
}
