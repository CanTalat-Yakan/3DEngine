using ImGuiNET;

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

    [Behavior]
    public struct HUDOverlay
    {
        [OnUpdate]
        public static void Draw(BehaviorContext ctx)
        {
            ImGui.Begin("HUD");
            ImGui.Text($"FPS: {(1.0 / ctx.Res<Time>().DeltaSeconds):0}");
            ImGui.End();
        }
    }

    [Behavior]
    public struct StaticLog
    {
        public float a;
        private float b { get; set; }

        [OnUpdate]
        public static void Tick(BehaviorContext ctx)
        {
            Console.WriteLine("Log");
        }
    }

    [Behavior]
    public struct Spawner
    {
        public float a;
        private float b { get; set; }

        [OnStartup]
        public static void Init(BehaviorContext ctx)
        {
            var e = ctx.Ecs.Spawn();
            ctx.Ecs.Add(e, new Spawner { a = 1.0f });
        }

        [OnUpdate]
        public void Tick(BehaviorContext ctx)
        {
            // Use instance state
            b += (float)ctx.Res<Time>().DeltaSeconds;
            Console.WriteLine($"Spawner running. a={a}, b={b}");
        }
    }
}