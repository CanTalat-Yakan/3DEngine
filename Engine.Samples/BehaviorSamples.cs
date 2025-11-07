using SDL3;
using ImGuiNET;

namespace Engine;

/// <summary> Position component used in samples. </summary>
public struct Position
{
    public float X, Y;
}

/// <summary> Velocity component used in samples. </summary>
public struct Velocity
{
    public float X, Y;
}

// Minimal behavior that increments X every frame using Time.DeltaSeconds
[Behavior]
public struct Mover
{
    public float Speed;

    /// <summary> One-time setup: spawn demo entities. </summary>
    [OnStartup]
    public void Init(BehaviorContext ctx)
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

    /// <summary> Move positions along X based on Speed and delta time. </summary>
    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var dt = (float)ctx.Res<Time>().DeltaSeconds;
        foreach (var (e, pos) in ecs.Query<Position>())
        {
            var p = pos;
            p.X += Speed * dt;
            ecs.Update(e, p);
        }
    }
}

// Reacts only when both Mover and Position exist on an entity
[Behavior]
public struct BounceOnEdges
{
    public float MinX;
    public float MaxX;

    /// <summary> Flips Mover.Speed when Position moves outside [MinX, MaxX]. </summary>
    [OnUpdate, With(typeof(Position))]
    public void Bounce(BehaviorContext ctx)
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

// Input driven behavior: toggles direction on Space key
[Behavior]
public struct InputController
{
    /// <summary> On Space pressed, invert Mover.Speed. </summary>
    [OnUpdate, With(typeof(Mover))]
    public void HandleInput(BehaviorContext ctx)
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
[Behavior]
public struct ResizeLogger
{
    /// <summary> Prints window resize events to stdout. </summary>
    [OnUpdate]
    public static void PumpResizeEvents(BehaviorContext ctx)
    {
        foreach (var evt in Events.Get<WindowResized>(ctx.World).Drain())
            System.Console.WriteLine($"[Resize] {evt.Width}x{evt.Height}");
    }
}

[Behavior]
public struct Gravity
{
    public float Strength;

    /// <summary> Applies gravity to Velocity and integrates Position. </summary>
    [OnUpdate, With(typeof(Velocity))]
    public void Apply(BehaviorContext ctx)
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

[Behavior]
public struct Friction
{
    public float Coefficient;

    /// <summary> Scales velocity by (1 - k*dt). </summary>
    [OnUpdate, With(typeof(Velocity))]
    public void Dampen(BehaviorContext ctx)
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

[Behavior]
public struct Lifetime
{
    public float Seconds;
    private float _accum;

    /// <summary> Despawns the entity after Seconds elapse. </summary>
    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        _accum += (float)ctx.Res<Time>().DeltaSeconds;
        if (_accum >= Seconds)
        {
            ctx.Cmd.Despawn(ctx.EntityId);
        }
    }
}

[Behavior]
public struct ChangedPositionLogger
{
    /// <summary> Logs Position when it changes this frame. </summary>
    [OnUpdate, With(typeof(Position))]
    public void Log(BehaviorContext ctx)
    {
        if (ctx.Ecs.Changed<Position>(ctx.EntityId))
        {
            var p = ctx.Ecs.TryGet<Position>(ctx.EntityId, out var pos) ? pos : default;
            System.Console.WriteLine($"Entity {ctx.EntityId} moved to ({p.X:0.00},{p.Y:0.00})");
        }
    }
}

[Behavior]
public struct Spawner
{
    public float Interval;
    private float _accum;

    /// <summary> Spawns a new demo entity every Interval seconds. </summary>
    [OnUpdate]
    public void Tick(BehaviorContext ctx)
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

[Behavior]
public struct HUDOverlay
{
    /// <summary> Shows a simple HUD with entity count and FPS. </summary>
    [OnRender]
    public static void Draw(BehaviorContext ctx)
    {
        ImGui.Begin("HUD");
        ImGui.Text($"Entities: approx {ctx.Res<EcsWorld>().Query<Position>().Count()}");
        ImGui.Text($"FPS: {(1.0 / ctx.Res<Time>().DeltaSeconds):0}");
        ImGui.End();
    }
}