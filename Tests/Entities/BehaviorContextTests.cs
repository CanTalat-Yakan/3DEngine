using FluentAssertions;
using Xunit;

namespace Engine.Tests.Entities;

[Trait("Category", "Unit")]
public class BehaviorContextTests
{
    // ── Constructor resolves resources ──────────────────────────────────

    [Fact]
    public void Constructor_Resolves_All_Required_Resources()
    {
        using var world = new World();
        var ecs = new EcsWorld();
        var cmd = new EcsCommands();
        var time = new Time();
        var input = new Input();
        world.InsertResource(ecs);
        world.InsertResource(cmd);
        world.InsertResource(time);
        world.InsertResource(input);

        var ctx = new BehaviorContext(world);

        ctx.World.Should().BeSameAs(world);
        ctx.Ecs.Should().BeSameAs(ecs);
        ctx.Cmd.Should().BeSameAs(cmd);
        ctx.Time.Should().BeSameAs(time);
        ctx.Input.Should().BeSameAs(input);
    }

    [Fact]
    public void Constructor_Throws_When_EcsWorld_Missing()
    {
        using var world = new World();
        world.InsertResource(new EcsCommands());
        world.InsertResource(new Time());
        world.InsertResource(new Input());

        var act = () => new BehaviorContext(world);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_Throws_When_EcsCommands_Missing()
    {
        using var world = new World();
        world.InsertResource(new EcsWorld());
        world.InsertResource(new Time());
        world.InsertResource(new Input());

        var act = () => new BehaviorContext(world);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_Throws_When_Time_Missing()
    {
        using var world = new World();
        world.InsertResource(new EcsWorld());
        world.InsertResource(new EcsCommands());
        world.InsertResource(new Input());

        var act = () => new BehaviorContext(world);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_Throws_When_Input_Missing()
    {
        using var world = new World();
        world.InsertResource(new EcsWorld());
        world.InsertResource(new EcsCommands());
        world.InsertResource(new Time());

        var act = () => new BehaviorContext(world);

        act.Should().Throw<InvalidOperationException>();
    }

    // ── EntityId ────────────────────────────────────────────────────────

    [Fact]
    public void EntityId_Defaults_To_Zero()
    {
        var ctx = CreateContext();

        ctx.EntityId.Should().Be(0);
    }

    [Fact]
    public void EntityId_Is_Settable()
    {
        var ctx = CreateContext();

        ctx.EntityId = 42;

        ctx.EntityId.Should().Be(42);
    }

    // ── Res<T> ──────────────────────────────────────────────────────────

    [Fact]
    public void Res_Returns_Resource_From_World()
    {
        using var world = new World();
        world.InsertResource(new EcsWorld());
        world.InsertResource(new EcsCommands());
        world.InsertResource(new Time());
        world.InsertResource(new Input());
        world.InsertResource("custom-resource");

        var ctx = new BehaviorContext(world);

        ctx.Res<string>().Should().Be("custom-resource");
    }

    [Fact]
    public void Res_Throws_When_Resource_Missing()
    {
        var ctx = CreateContext();

        var act = () => ctx.Res<double>();

        act.Should().Throw<InvalidOperationException>();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static BehaviorContext CreateContext()
    {
        var world = new World();
        world.InsertResource(new EcsWorld());
        world.InsertResource(new EcsCommands());
        world.InsertResource(new Time());
        world.InsertResource(new Input());
        return new BehaviorContext(world);
    }
}

