namespace Engine;

/// <summary>Application entry point.</summary>
public sealed class Program
{
    [STAThread]
    private static void Main()
    {
        new App(Config.GetDefault())
            .AddPlugin(new DefaultPlugins())
            .Run();
    }
}