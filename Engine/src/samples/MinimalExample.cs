namespace Engine;

public static class MinimalExampleProgram
{
    public static void Run()
    {
        var config = Config.GetDefault(multiSample: MultiSample.x4, defaultBoot: true);
        new App(config)
            .AddPlugins(new DefaultPlugins())
            .AddPlugin(new MinimalExample())
            .Run();
    }
}

