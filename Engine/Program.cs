using System.Numerics;
using ImGuiNET;
using SDL3;

namespace Engine;

public sealed class Program
{
    [STAThread]
    private static void Main()
    {
        var config = Config.GetDefault();
        new App(config)
            .AddPlugins(new DefaultPlugins())
            .Run();
    }
}