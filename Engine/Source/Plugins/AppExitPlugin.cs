namespace Engine;

public sealed class AppExitPlugin : IPlugin
{
    public void Build(App app)
    {
        if (!app.World.ContainsResource<AppExit>())
            app.World.InsertResource(new AppExit());

        var window = app.World.Resource<AppWindow>();
        window.QuitEvent += () => app.World.Resource<AppExit>().Requested = true;

        app.AddSystem(Stage.First, (World w) =>
        {
            if (w.Resource<AppExit>().Requested)
                w.Resource<AppWindow>().RequestClose();
        });
    }
}

public sealed class AppExit
{
    public bool Requested;
}
