namespace Engine;

public sealed class EcsPlugin : IPlugin
{
    public void Build(App app)
    {
        if (!app.World.ContainsResource<EcsWorld>())
            app.World.InsertResource(new EcsWorld());
        if (!app.World.ContainsResource<EcsCommands>())
            app.World.InsertResource(new EcsCommands());

        app.AddSystem(Stage.PostUpdate, (World w) => w.Resource<EcsCommands>().Apply(w.Resource<EcsWorld>()));
    }
}
