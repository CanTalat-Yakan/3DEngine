namespace Engine;

public sealed class Program
{
    [STAThread]
    private static void Main()
    {
        new App(Config.GetDefault())
            .AddPlugins(new DefaultPlugins())
            .Run();
    }
}