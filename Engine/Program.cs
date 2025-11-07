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
    public struct FpsOverlay
    {
        private static double _highestFps;        
        
        [OnRender]
        public static void Draw(BehaviorContext ctx)
        {
            ImGui.Begin("HUD");
            var fps = 1.0 / ctx.Res<Time>().DeltaSeconds;
            if (fps > _highestFps) _highestFps = fps;
            ImGui.Text($"FPS: {fps:0}");
            ImGui.Text($"HighestFPS: {_highestFps:0}");
            ImGui.End();
        }
    }
}