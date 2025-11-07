namespace Engine;

/// <summary>Creates the application window from Config and inserts it as a resource.</summary>
public sealed class WindowPlugin : IPlugin
{
    /// <summary>Creates and shows the AppWindow using Config, then inserts it into the World.</summary>
    public void Build(App app)
    {
        var config = app.World.Resource<Config>();
        var window = new AppWindow(config.WindowData);
        window.Show(config.WindowCommand);
        app.World.InsertResource(window);
    }
}

/// <summary>Provides application exit handling: listens for window quit events and requests closure via a resource flag.</summary>
public sealed class AppExitPlugin : IPlugin
{
    /// <summary>Inserts the <see cref="AppExit"/> resource (if missing), wires window quit to set its flag, and adds a First-stage system to close the window when requested.</summary>
    public void Build(App app)
    {
        // Ensure exit state resource exists.
        if (!app.World.ContainsResource<AppExit>())
            app.World.InsertResource(new AppExit());

        // When the window signals quit, raise the Requested flag.
        var window = app.World.Resource<AppWindow>();
        window.QuitEvent += () => app.World.Resource<AppExit>().Requested = true;

        // Early frame: if an exit was requested previously, ask window to close (will break main loop).
        app.AddSystem(Stage.First, (World world) =>
        {
            if (world.Resource<AppExit>().Requested)
                world.Resource<AppWindow>().RequestClose();
        });
    }
}

/// <summary>Resource tracking whether an application exit was requested.</summary>
public sealed class AppExit
{
    /// <summary>True if a quit event was observed and the app should close.</summary>
    public bool Requested;
}
