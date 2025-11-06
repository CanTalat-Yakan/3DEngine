using Engine.Behaviour;
using SDL3;
using ImGuiNET;

namespace Engine.Samples
{
    public struct Position { public float X, Y; }
    public struct Velocity { public float X, Y; }
    
    // Minimal behaviour that increments X every frame using Time.DeltaSeconds
    [Behaviour]
    public partial struct Mover
    {
        public float Speed;

        [OnStartup]
        public void Init(BehaviourContext ctx)
        {
            if (Speed <= 0) Speed = 50f;
            var baseSpeed = Speed; // avoid capturing 'this' inside lambda
            // Spawn 10 entities with Position for demo
            for (int i = 0; i < 10; i++)
            {
                int idx = i; // avoid closure pitfalls
                ctx.Cmd.Spawn((e, world) =>
                {
                    world.Add(e, new Position { X = idx * 10, Y = 0 });
                    world.Add(e, new Velocity { X = 0, Y = 0 });
                    world.Add(e, new Mover { Speed = baseSpeed + idx });
                    world.Add(e, new BounceOnEdges { MinX = -100, MaxX = 300 });
                    world.Add(e, new Lifetime { Seconds = 30f });
                });
            }
        }

        [OnUpdate]
        public void Tick(BehaviourContext ctx)
        {
            var ecs = ctx.Ecs;
            var dt = (float)ctx.Res<Time>().DeltaSeconds;
            foreach (var (e, pos) in ecs.Query<Position>())
            {
                // Move to the right over time
                var p = pos;
                p.X += Speed * dt;
                ecs.Update(e, p);
            }
        }
    }

    // Reacts only when both Mover and Position exist on an entity
    [Behaviour]
    public partial struct BounceOnEdges
    {
        public float MinX;
        public float MaxX;

        [OnUpdate, With(typeof(Position))]
        public void Bounce(BehaviourContext ctx)
        {
            var ecs = ctx.Ecs;
            if (ecs.TryGet<Position>(ctx.EntityId, out var pos))
            {
                var p = pos;
                if (p.X < MinX || p.X > MaxX)
                {
                    if (ecs.TryGet<Mover>(ctx.EntityId, out var mover))
                    {
                        var m = mover;
                        m.Speed *= -1f;
                        ecs.Update(ctx.EntityId, m);
                    }
                }
            }
        }
    }

    // Input driven behaviour: toggles direction on Space key
    [Behaviour]
    public partial struct InputController
    {
        [OnUpdate, With(typeof(Mover))]
        public void HandleInput(BehaviourContext ctx)
        {
            var input = ctx.Res<Input>();
            if (input.KeyPressed(SDL.Scancode.Space))
            {
                if (ctx.Ecs.TryGet<Mover>(ctx.EntityId, out var mover))
                {
                    var m = mover;
                    m.Speed *= -1f;
                    ctx.Ecs.Update(ctx.EntityId, m);
                }
            }
        }
    }

    // Window resize feedback
    [Behaviour]
    public partial struct ResizeLogger
    {
        [OnUpdate]
        public static void PumpResizeEvents(BehaviourContext ctx)
        {
            foreach (var evt in Events.Get<WindowResized>(ctx.World).Drain())
                System.Console.WriteLine($"[Resize] {evt.Width}x{evt.Height}");
        }
    }

    [Behaviour]
    public partial struct Gravity
    {
        public float Strength;
        [OnUpdate, With(typeof(Velocity))]
        public void Apply(BehaviourContext ctx)
        {
            var dt = (float)ctx.Res<Time>().DeltaSeconds;
            if (ctx.Ecs.TryGet<Velocity>(ctx.EntityId, out var vel))
            {
                var v = vel;
                v.Y += Strength * dt;
                ctx.Ecs.Update(ctx.EntityId, v);
            }
            if (ctx.Ecs.TryGet<Position>(ctx.EntityId, out var pos))
            {
                var p = pos;
                if (ctx.Ecs.TryGet<Velocity>(ctx.EntityId, out var vel2))
                {
                    p.Y += vel2.Y * dt;
                    ctx.Ecs.Update(ctx.EntityId, p);
                }
            }
        }
    }

    [Behaviour]
    public partial struct Friction
    {
        public float Coefficient;
        [OnUpdate, With(typeof(Velocity))]
        public void Dampen(BehaviourContext ctx)
        {
            var dt = (float)ctx.Res<Time>().DeltaSeconds;
            if (ctx.Ecs.TryGet<Velocity>(ctx.EntityId, out var vel))
            {
                var v = vel;
                v.X *= 1f - Coefficient * dt;
                v.Y *= 1f - Coefficient * dt;
                ctx.Ecs.Update(ctx.EntityId, v);
            }
        }
    }

    [Behaviour]
    public partial struct Lifetime
    {
        public float Seconds;
        private float _accum;

        [OnUpdate]
        public void Tick(BehaviourContext ctx)
        {
            _accum += (float)ctx.Res<Time>().DeltaSeconds;
            if (_accum >= Seconds)
            {
                ctx.Cmd.Despawn(ctx.EntityId);
            }
        }
    }

    [Behaviour]
    public partial struct ChangedPositionLogger
    {
        [OnUpdate, With(typeof(Position))]
        public void Log(BehaviourContext ctx)
        {
            if (ctx.Ecs.Changed<Position>(ctx.EntityId))
            {
                var p = ctx.Ecs.TryGet<Position>(ctx.EntityId, out var pos) ? pos : default;
                System.Console.WriteLine($"Entity {ctx.EntityId} moved to ({p.X:0.00},{p.Y:0.00})");
            }
        }
    }

    [Behaviour]
    public partial struct Spawner
    {
        public float Interval;
        private float _accum;

        [OnUpdate]
        public void Tick(BehaviourContext ctx)
        {
            _accum += (float)ctx.Res<Time>().DeltaSeconds;
            if (_accum >= Interval)
            {
                _accum = 0;
                ctx.Cmd.Spawn((e, world) =>
                {
                    world.Add(e, new Position { X = 0, Y = 0 });
                    world.Add(e, new Velocity { X = 100, Y = -50 });
                    world.Add(e, new Mover { Speed = 25 });
                    world.Add(e, new Gravity { Strength = 9.81f });
                    world.Add(e, new Friction { Coefficient = 0.1f });
                    world.Add(e, new BounceOnEdges { MinX = -200, MaxX = 400 });
                    world.Add(e, new Lifetime { Seconds = 10 });
                });
            }
        }
    }

    [Behaviour]
    public partial struct HUDOverlay
    {
        [OnRender]
        public static void Draw(BehaviourContext ctx)
        {
            ImGui.Begin("HUD");
            ImGui.Text($"Entities: approx {ctx.Res<EcsWorld>().Query<Position>().Count()}");
            ImGui.Text($"FPS: {(1.0/ctx.Res<Time>().DeltaSeconds):0}");
            ImGui.End();
        }
    }
}
