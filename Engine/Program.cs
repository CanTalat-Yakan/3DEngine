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
            var time = ctx.Res<Time>();
            var delta = time.DeltaSeconds;
            var fps = 1.0 / delta;
            if (fps > _highestFps) _highestFps = fps;
            ImGui.Text($"FPS: {fps:0}");
            ImGui.Text($"HighestFPS: {_highestFps:0}");
            if (time.ElapsedSeconds % 1.0 < delta) _highestFps = 0;
            ImGui.End();
        }
    }
}