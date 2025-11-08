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
            var delta = ctx.Time.DeltaSeconds;
            var fps = 1.0 / delta;
            if (ctx.Time.ElapsedSeconds % 1.0 < delta) _highestFps = 0; // Reset peak roughly once per second
            if (fps > _highestFps) _highestFps = fps; // Track peak FPS within the rolling one-second window
            ImGui.Begin("HUD");
            ImGui.Text($"FPS: {fps:0}");
            ImGui.Text($"Delta: {delta:0.000}");
            ImGui.Text($"HighestFPS: {_highestFps:0}");
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
            ImGui.Text($"Entities: {ctx.Ecs.Count<CounterComponent>()}");
            ImGui.End();
            // foreach (var rc in ctx.Ecs.IterateRef<CounterComponent>()) { }
            // int posCount = ctx.Ecs.Count<CounterComponent>();
            // var posEntities = ctx.Ecs.EntitiesWith<position>();
        }
    }

    [Behavior]
    public struct SpawnEntitiesOnSpace
    {
        private static bool _spawned;

        [OnUpdate]
        public static void Update(BehaviorContext ctx)
        {
            if (!ctx.Input.KeyDown(SDL.Scancode.Space) || _spawned)
                return;

            _spawned = true;

            for (int i = 0; i < 100_000; i++)
            {
                var e = ctx.Ecs.SpawnEntity();
                ctx.Ecs.Add(e.Id, new CounterComponent()); // keep Add(int, ...) for now
            }
        }

        [OnPostUpdate]
        public static void LateUpdate(BehaviorContext ctx)
        {
            if (ctx.Time.ElapsedSeconds % 1.0 < ctx.Time.DeltaSeconds)
                _spawned = false;
        }
    }
}