namespace Engine;

/// <summary>Handles application exit: listens for window quit events and requests closure.</summary>
public sealed class AppExitPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.AppExit");

    /// <summary>Inserts the <see cref="AppExit"/> resource (if missing), wires window quit to set its flag, and adds a First-stage system to close the window when requested.</summary>
    public void Build(App app)
    {
        Logger.Info("AppExitPlugin: Registering exit handler...");
        // Ensure exit state resource exists.
        app.World.InitResource<AppExit>();

        // When the window signals quit, raise the Requested flag.
        var window = app.World.Resource<AppWindow>();
        window.QuitEvent += () =>
        {
            Logger.Info("Quit event received — flagging application exit.");
            app.World.Resource<AppExit>().Requested = true;
        };

        // Early frame: if an exit was requested previously, ask window to close (will break main loop).
        app.AddSystem(Stage.First, (World world) =>
        {
            if (world.Resource<AppExit>().Requested)
            {
                Logger.Info("Exit requested — closing window to break main loop.");
                world.Resource<AppWindow>().RequestClose();
            }
        });

        Logger.Info("AppExitPlugin: Exit handler registered.");
    }
}

/// <summary>Resource tracking whether an application exit was requested.</summary>
public sealed class AppExit
{
    /// <summary>True if a quit event was observed and the app should close.</summary>
    public bool Requested;
}
