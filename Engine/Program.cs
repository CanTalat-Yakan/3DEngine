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
    public partial struct Spawner
    {
        public float Interval;
        private float _accum;

        [OnUpdate]
        public static void Tick(BehaviorContext ctx)
        {
            Console.WriteLine("Test");
        }
    }
}