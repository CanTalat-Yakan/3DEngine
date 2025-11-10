namespace Engine;

/// <summary>Fixed execution phases processed in a strict order each frame.</summary>
public enum Stage
{
    Startup,
    First,
    PreUpdate,
    Update,
    PostUpdate,
    Render,
    Last,
    Cleanup,
}

/// <summary>Provides ordered list of stages for iteration.</summary>
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
        Stage.Cleanup,
    };

    /// <summary>Returns enumerable of stages in execution order.</summary>
    public static IEnumerable<Stage> AllInOrder() => Ordered;
}
