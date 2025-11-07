namespace Engine;

// Design-time stub; source generator supplies partial static method implementation.
public sealed partial class BehaviorsPlugin : IPlugin
{
    public void Build(App app)
    {
        // Invoke generated registrations if any exist.
        BuildGenerated(app);
    }

    // Implemented by source generator (may be empty if no behaviors found)
    static partial void BuildGenerated(App app);
}

