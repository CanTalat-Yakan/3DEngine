using ImGuiNET;
using SDL3;

namespace Engine;

/// <summary>Application entry point. Configures and runs the engine with default plugins.</summary>
public sealed class Program
{
    [STAThread]
    private static void Main()
    {
        // Build app with default configuration and plugins, then start the main loop.
        new App(Config.GetDefault())
            .AddPlugin(new DefaultPlugins())
            .Run();
    }

    /// <summary>Simple HUD overlay that displays current and peak FPS for the last second.</summary>
    [Behavior]
    public struct FpsOverlay
    {
        private static double _highestFps;

        /// <summary>Draws the FPS overlay in an ImGui window each render frame.</summary>
        [OnRender]
        public static void Draw(BehaviorContext ctx)
        {
            ImGui.Begin("HUD");
            var time = ctx.Res<Time>();
            var delta = time.DeltaSeconds;
            var fps = 1.0 / delta;
            if (fps > _highestFps) _highestFps = fps; // Track peak FPS within the rolling one-second window
            ImGui.Text($"FPS: {fps:0}");
            ImGui.Text($"HighestFPS: {_highestFps:0}");
            if (time.ElapsedSeconds % 1.0 < delta) _highestFps = 0; // Reset peak roughly once per second
            ImGui.End();
        }
    }

    [Behavior]
    public struct CounterComponent()
    {
        private static int _count;

        [OnUpdate]
        public void Tick(BehaviorContext ctx)
        {
            _count++;
        }

        [OnRender]
        public static void Draw(BehaviorContext ctx)
        {
            ImGui.Begin("HUD");
            ImGui.Text($"Count: {_count}");
            ImGui.Text($"Entities: {ctx.Ecs.Query<CounterComponent>().Count()}");
            ImGui.End();
        }
    }

    [Behavior]
    public struct SpawnEntitiesOnSpace
    {
        private static bool _spawned;

        [OnUpdate]
        public static void Update(BehaviorContext ctx)
        {
            var input = ctx.Res<Input>();
            if (!input.KeyDown(SDL.Scancode.Space) || _spawned)
                return;

            _spawned = true;

            var ecs = ctx.Res<EcsWorld>();
            for (int i = 0; i < 100_000; i++)
            {
                var e = ecs.Spawn();
                ecs.Add(e, new CounterComponent());
            }
        }

        [OnPostUpdate]
        public static void LateUpdate(BehaviorContext ctx)
        {
            var time = ctx.Res<Time>();
            if (time.ElapsedSeconds % 1.0 < time.DeltaSeconds)
                _spawned = false;
        }
    }
}