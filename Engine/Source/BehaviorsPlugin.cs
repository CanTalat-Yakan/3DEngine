namespace Engine;

/// <summary>Design-time stub for behavior registration; the source generator provides the partial implementation of BuildGenerated(App) to register discovered behaviors.</summary>
public sealed partial class BehaviorsPlugin : IPlugin
{
    /// <summary>Invokes the source-generated registration method for discovered behaviors.</summary>
    public void Build(App app)
    {
        BuildGenerated(app);
    }

    /// <summary>Implemented by the source generator; registers all behaviors found at compile time. May be empty if no behaviors exist in the compilation.</summary>
    static partial void BuildGenerated(App app);
}
