using System;
using System.Linq;
using Xunit;

namespace Engine.Tests;

public struct TestComp
{
    public int A;
}

public class EcsWorldTests
{
    [Fact]
    public void Spawn_Add_Query_Work()
    {
        var ecs = new Engine.EcsWorld();
        var e1 = ecs.Spawn();
        var e2 = ecs.Spawn();
        ecs.Add(e1, new TestComp { A = 1 });
        ecs.Add(e2, new TestComp { A = 2 });

        var result = ecs.Query<TestComp>().OrderBy(t => t.Entity).ToArray();
        Assert.Equal(2, result.Length);
        Assert.Equal((e1, new TestComp { A = 1 }), (result[0].Entity, result[0].Component));
        Assert.Equal((e2, new TestComp { A = 2 }), (result[1].Entity, result[1].Component));
    }

    [Fact]
    public void Update_Marks_Changed_For_Current_Frame()
    {
        var ecs = new Engine.EcsWorld();
        var e = ecs.Spawn();
        ecs.Add(e, new TestComp { A = 0 });

        // Tick 1
        ecs.BeginFrame();
        ecs.Update(e, new TestComp { A = 1 });
        Assert.True(ecs.Changed<TestComp>(e));
        Assert.Equal(1, ecs.Query<TestComp>().Single().Component.A);

        // Next frame: changed must reset
        ecs.BeginFrame();
        Assert.False(ecs.Changed<TestComp>(e));
    }

    [Fact]
    public void Mutate_Transforms_And_Marks_Changed()
    {
        var ecs = new Engine.EcsWorld();
        var e = ecs.Spawn();
        ecs.Add(e, new TestComp { A = 5 });

        ecs.BeginFrame();
        ecs.Mutate<TestComp>(e, c => { c.A += 3; return c; });
        Assert.True(ecs.Changed<TestComp>(e));
        Assert.Equal(8, ecs.Query<TestComp>().Single().Component.A);
    }

    [Fact]
    public void TransformEach_Updates_All_And_Marks_Changed()
    {
        var ecs = new Engine.EcsWorld();
        var e1 = ecs.Spawn();
        var e2 = ecs.Spawn();
        ecs.Add(e1, new TestComp { A = 1 });
        ecs.Add(e2, new TestComp { A = 2 });

        ecs.BeginFrame();
        ecs.TransformEach<TestComp>((id, c) => { c.A += id == e1 ? 10 : 20; return c; });

        var dict = ecs.Query<TestComp>().ToDictionary(t => t.Entity, t => t.Component.A);
        Assert.Equal(11, dict[e1]);
        Assert.Equal(22, dict[e2]);
        Assert.True(ecs.Changed<TestComp>(e1));
        Assert.True(ecs.Changed<TestComp>(e2));
    }

    [Fact]
    public void TryGet_Returns_Existing_Value()
    {
        var ecs = new Engine.EcsWorld();
        var e = ecs.Spawn();
        ecs.Add(e, new TestComp { A = 42 });
        Assert.True(ecs.TryGet<TestComp>(e, out var comp));
        Assert.Equal(42, comp.A);
    }

    [Fact]
    public void Query_Two_And_Three_Components()
    {
        var ecs = new Engine.EcsWorld();
        var e1 = ecs.Spawn();
        var e2 = ecs.Spawn();
        ecs.Add(e1, new TestComp { A = 1 });
        ecs.Add(e2, new TestComp { A = 2 });
        ecs.Add(e1, new OtherComp { B = 3 });
        ecs.Add(e2, new OtherComp { B = 4 });
        ecs.Add(e2, new ThirdComp { C = 9 });

        var q2 = ecs.Query<TestComp, OtherComp>().OrderBy(t => t.Entity).ToArray();
        Assert.Equal(2, q2.Length);
        Assert.Equal(1, q2[0].C1.A);
        Assert.Equal(3, q2[0].C2.B);

        var q3 = ecs.Query<TestComp, OtherComp, ThirdComp>().ToArray();
        Assert.Single(q3);
        Assert.Equal(e2, q3[0].Entity);
        Assert.Equal(2, q3[0].C1.A);
        Assert.Equal(4, q3[0].C2.B);
        Assert.Equal(9, q3[0].C3.C);
    }

    public struct OtherComp { public int B; }
    public struct ThirdComp { public int C; }
}
