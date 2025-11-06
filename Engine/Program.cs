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
        [OnRender]
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
        [OnUpdate]
        public static void Tick(BehaviorContext ctx)
        {
            // Console.WriteLine("Log");
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
            // Console.WriteLine($"Spawner running. a={a}, b={b}");
        }
    }

    public class SomeDisposable : IDisposable
    {
        private float num = 2;

        public string Log() => num.ToString();
        
        public void Dispose()
        {
            // Cleanup resources
        }
    }

    [Behavior]
    public struct HeavyBehavior : IDisposable
    {
        private SomeDisposable? _handle;

        [OnStartup]
        public static void Init(BehaviorContext ctx)
        {
            var e = ctx.Ecs.Spawn();
            ctx.Ecs.Add(e, new HeavyBehavior { _handle = new SomeDisposable() });
        }

        [OnUpdate]
        public void Tick(BehaviorContext ctx)
        {
            Console.WriteLine(_handle.Log());
            // use _handle...
        }

        public void Dispose()
        {
            _handle?.Dispose();
            _handle = null;
        }
    }
}