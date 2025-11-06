namespace Engine;

public sealed class ECSPlugin : IPlugin
{
    public void Build(App app)
    {
        if (!app.World.ContainsResource<ECSWorld>())
            app.World.InsertResource(new ECSWorld());
        if (!app.World.ContainsResource<ECSCommands>())
            app.World.InsertResource(new ECSCommands());

        app.AddSystem(Stage.PostUpdate, (World w) => w.Resource<ECSCommands>().Apply(w.Resource<ECSWorld>()));
    }
}
