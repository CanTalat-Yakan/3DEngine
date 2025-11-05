namespace Engine;

public sealed class ExceptionHandlingPlugin : IPlugin
{
    public void Build(App app)
    {
        if (!app.World.ContainsResource<ExceptionHandlerInstalled>())
        {
            Install();
            app.World.InsertResource(new ExceptionHandlerInstalled());
        }
    }

    private static void Install()
    {
        var rootPath = AppContext.BaseDirectory;
        var logFilePath = rootPath + "Application.log";

        ExceptionHandler.CreateTraceLog(rootPath, logFilePath);

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception exception)
                ExceptionHandler.HandleException(exception);
        };
    }
}

public sealed class ExceptionHandlerInstalled { }

