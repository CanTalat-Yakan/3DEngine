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
        private static double _lowestFps;        
        private static double _highestDelta;        
        private static double _lowestDelta;        
        
        [OnRender]
        public static void Draw(BehaviorContext ctx)
        {
            ImGui.Begin("HUD");
            var time = ctx.Res<Time>();
            var delta = time.DeltaSeconds;
            var fps = 1.0 / time.DeltaSeconds;
            if (_lowestFps == 0) _lowestFps = fps;
            if (_lowestDelta == 0) _lowestDelta = delta;
            if (delta > _highestDelta) _highestDelta = delta;
            if (delta < _lowestDelta) _lowestDelta = delta;
            if (fps > _highestFps) _highestFps = fps;
            if (fps < _lowestFps) _lowestFps = fps;
            ImGui.Text($"FPS: {fps:0}");
            ImGui.Text($"Delta: {delta}");
            ImGui.Text($"LowestFPS: {_lowestFps:0}");
            ImGui.Text($"HighestFPS: {_highestFps:0}");
            ImGui.Text($"LowestDelta: {_lowestDelta}");
            ImGui.Text($"HighestDelta: {_highestDelta}");
            
            // Every second reset min/max
            if (time.ElapsedSeconds % 1.0 < time.DeltaSeconds)
            {
                _highestFps = 0;
                _lowestFps = fps;
                _highestDelta = 0;
                _lowestDelta = delta;
            }
            
            ImGui.End();
        }
    }
}