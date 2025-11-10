using Xunit;

namespace Engine.Tests;

public struct TestComp
{
    public int A;
}

public struct OtherComp
{
    public int B;
}

public struct ThirdComp
{
    public int C;
}

public sealed class DisposableComp : IDisposable
{
    public int DisposedCount;
    public bool IsDisposed => DisposedCount > 0;
    public void Dispose() => DisposedCount++;
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
        ecs.Mutate<TestComp>(e, c =>
        {
            c.A += 3;
            return c;
        });
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
        ecs.TransformEach<TestComp>((id, c) =>
        {
            c.A += id == e1 ? 10 : 20;
            return c;
        });

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

    [Fact]
    public void GetRef_ModifyComponent_ReflectsInQuery()
    {
        var ecs = new Engine.EcsWorld();
        var e = ecs.Spawn();
        ecs.Add(e, new TestComp { A = 10 });
        ref var comp = ref ecs.GetRef<TestComp>(e);
        comp.A += 5; // mutate via ref
        Assert.Equal(15, ecs.Query<TestComp>().Single().Component.A);
        Assert.True(ecs.TryGet<TestComp>(e, out var c2) && c2.A == 15);
    }

    [Fact]
    public void GetRef_Throws_For_Missing_Component()
    {
        var ecs = new Engine.EcsWorld();
        var e = ecs.Spawn();
        Assert.Throws<KeyNotFoundException>(() =>
        {
            ref var _ = ref ecs.GetRef<TestComp>(e);
        });
    }

    [Fact]
    public void GetSpan_MutateAll_MarksChanged()
    {
        var ecs = new Engine.EcsWorld();
        int e1 = ecs.Spawn();
        int e2 = ecs.Spawn();
        ecs.Add(e1, new TestComp { A = 2 });
        ecs.Add(e2, new TestComp { A = 3 });
        ecs.BeginFrame();
        var span = ecs.GetSpan<TestComp>();
        Assert.True(span.IsValid);
        for (int i = 0; i < span.Entities.Length; i++)
        {
            span.Components[i].A *= 2;
        }

        // Mark changes manually via TransformEach or Update - direct span mutation does not auto mark changed
        ecs.TransformEach<TestComp>((id, c) => c); // apply tick marking without changing values
        Assert.True(ecs.Changed<TestComp>(e1));
        Assert.True(ecs.Changed<TestComp>(e2));
        var arr = ecs.Query<TestComp>().OrderBy(t => t.Entity).ToArray();
        Assert.Equal(4, arr[0].Component.A);
        Assert.Equal(6, arr[1].Component.A);
    }
    
    [Fact]
    public void Despawn_Disposes_Disposable_Components_And_Reuses_EntityId()
    {
        var ecs = new Engine.EcsWorld();
        int e1 = ecs.Spawn();
        var disp = new DisposableComp();
        ecs.Add(e1, disp);
        ecs.Despawn(e1);
        Assert.True(disp.IsDisposed);
        int e2 = ecs.Spawn();
        Assert.Equal(e1, e2); // free list reuse
    }

    [Fact]
    public void QueryWhere_Filters_Correctly()
    {
        var ecs = new Engine.EcsWorld();
        int e1 = ecs.Spawn();
        int e2 = ecs.Spawn();
        int e3 = ecs.Spawn();
        ecs.Add(e1, new TestComp { A = 1 });
        ecs.Add(e2, new TestComp { A = 5 });
        ecs.Add(e3, new TestComp { A = 10 });
        var filtered = ecs.QueryWhere<TestComp>(c => c.A >= 5).OrderBy(t => t.Entity).ToArray();
        Assert.Equal(2, filtered.Length);
        Assert.Equal(e2, filtered[0].Entity);
        Assert.Equal(e3, filtered[1].Entity);
    }

    [Fact]
    public void Changed_Reset_After_Frame_When_ModifiedViaUpdate()
    {
        var ecs = new Engine.EcsWorld();
        int e = ecs.Spawn();
        ecs.Add(e, new TestComp { A = 7 });
        ecs.BeginFrame();
        ecs.Update(e, new TestComp { A = 8 });
        Assert.True(ecs.Changed<TestComp>(e));
        ecs.BeginFrame();
        Assert.False(ecs.Changed<TestComp>(e));
    }

    [Fact]
    public void Mutate_NoOp_On_Missing_Component_Does_Not_Throw()
    {
        var ecs = new Engine.EcsWorld();
        int e = ecs.Spawn();
        ecs.Mutate<TestComp>(e, c =>
        {
            c.A++;
            return c;
        }); // should silently do nothing
        Assert.False(ecs.TryGet<TestComp>(e, out _));
    }

    [Fact]
    public void IterateRef_ModifiesComponents()
    {
        var ecs = new Engine.EcsWorld();
        int e1 = ecs.Spawn();
        int e2 = ecs.Spawn();
        ecs.Add(e1, new TestComp { A = 1 });
        ecs.Add(e2, new TestComp { A = 2 });
        ecs.BeginFrame();
        foreach (var rc in ecs.IterateRef<TestComp>())
        {
            rc.Component.A += 5;
        }

        Assert.True(ecs.Changed<TestComp>(e1));
        Assert.True(ecs.Changed<TestComp>(e2));
        var values = ecs.Query<TestComp>().OrderBy(x => x.Entity).Select(x => x.Component.A).ToArray();
        Assert.Equal(new[] { 6, 7 }, values);
    }

    [Fact]
    public void IterateRef_TwoComponents_ModifiesBoth()
    {
        var ecs = new Engine.EcsWorld();
        int e1 = ecs.Spawn();
        int e2 = ecs.Spawn();
        ecs.Add(e1, new TestComp { A = 10 });
        ecs.Add(e1, new OtherComp { B = 1 });
        ecs.Add(e2, new TestComp { A = 20 });
        ecs.Add(e2, new OtherComp { B = 2 });
        ecs.BeginFrame();
        foreach (var rc in ecs.IterateRef<TestComp, OtherComp>())
        {
            rc.C1.A += rc.C2.B * 10; // 10+1*10=20, 20+2*10=40
            rc.C2.B += 3; // mutate second component
        }

        var q = ecs.Query<TestComp, OtherComp>().OrderBy(t => t.Entity).ToArray();
        Assert.Equal(2, q.Length);
        Assert.Equal(20, q[0].C1.A);
        Assert.Equal(4, q[0].C2.B); // 1+3
        Assert.Equal(40, q[1].C1.A);
        Assert.Equal(5, q[1].C2.B); // 2+3
        Assert.True(ecs.Changed<TestComp>(e1));
        Assert.True(ecs.Changed<TestComp>(e2));
        Assert.True(ecs.Changed<OtherComp>(e1));
        Assert.True(ecs.Changed<OtherComp>(e2));
    }

    [Fact]
    public void ParallelTransformEach_MarksChanged()
    {
        var ecs = new Engine.EcsWorld();
        for (int i = 0; i < 1000; i++)
        {
            int e = ecs.Spawn();
            ecs.Add(e, new TestComp { A = i });
        }

        ecs.BeginFrame();
        ecs.ParallelTransformEach<TestComp>((e, c) =>
        {
            c.A += 1;
            return c;
        });
        // spot check
        Assert.True(ecs.Changed<TestComp>(1));
        Assert.True(ecs.Changed<TestComp>(500));
        // ensure update happened
        var first = ecs.Query<TestComp>().First(t => t.Component.A == 1); // original 0 + 1
        Assert.Equal(1, first.Component.A);
    }

    [Fact]
    public void Remove_Component_RemovesOnlyThatComponent()
    {
        var ecs = new Engine.EcsWorld();
        int e = ecs.Spawn();
        ecs.Add(e, new TestComp { A = 5 });
        ecs.Add(e, new OtherComp { B = 9 });
        Assert.True(ecs.Has<TestComp>(e));
        Assert.True(ecs.Has<OtherComp>(e));
        Assert.True(ecs.Remove<OtherComp>(e));
        Assert.True(ecs.Has<TestComp>(e));
        Assert.False(ecs.Has<OtherComp>(e));
    }

    [Fact]
    public void EntitiesWith_Returns_All_Component_Entities()
    {
        var ecs = new Engine.EcsWorld();
        int e1 = ecs.Spawn();
        int e2 = ecs.Spawn();
        int e3 = ecs.Spawn();
        ecs.Add(e1, new TestComp { A = 1 });
        ecs.Add(e3, new TestComp { A = 3 });
        var entities = ecs.EntitiesWith<TestComp>().ToArray();
        Array.Sort(entities);
        Assert.Equal(new[] { e1, e3 }, entities);
    }

    [Fact]
    public void Changed_Reset_After_Remove_And_ReAdd()
    {
        var ecs = new Engine.EcsWorld();
        int e = ecs.Spawn();
        ecs.Add(e, new TestComp { A = 1 });
        ecs.BeginFrame();
        ecs.Update(e, new TestComp { A = 2 });
        Assert.True(ecs.Changed<TestComp>(e));
        ecs.BeginFrame();
        Assert.False(ecs.Changed<TestComp>(e));
        ecs.Remove<TestComp>(e);
        ecs.Add(e, new TestComp { A = 3 });
        ecs.BeginFrame();
        ecs.Update(e, new TestComp { A = 4 });
        Assert.True(ecs.Changed<TestComp>(e));
    }

    [Fact]
    public void SwapBackRemoval_Preserves_ChangedFlag_ForMovedComponent()
    {
        var ecs = new Engine.EcsWorld();
        int e1 = ecs.Spawn();
        int e2 = ecs.Spawn();
        ecs.Add(e1, new TestComp { A = 1 });
        ecs.Add(e2, new TestComp { A = 2 });
        ecs.BeginFrame();
        ecs.Update(e2, new TestComp { A = 3 }); // mark e2 changed
        Assert.True(ecs.Changed<TestComp>(e2));
        ecs.BeginFrame(); // clear change flags
        Assert.False(ecs.Changed<TestComp>(e2));
        ecs.Update(e2, new TestComp { A = 4 }); // mark again
        Assert.True(ecs.Changed<TestComp>(e2));
        // Remove e1 causing swap-back of e2 to index0
        ecs.Remove<TestComp>(e1);
        // e2 should still have its changed bit
        Assert.True(ecs.Changed<TestComp>(e2));
    }

    [Fact]
    public void SpanMutation_Without_Marking_Does_Not_Set_Changed()
    {
        var ecs = new Engine.EcsWorld();
        int e = ecs.Spawn();
        ecs.Add(e, new TestComp { A = 10 });
        ecs.BeginFrame();
        var span = ecs.GetSpan<TestComp>();
        span.Components[0].A = 99; // direct memory write without marking
        // Changed should still be false until explicit marking
        Assert.False(ecs.Changed<TestComp>(e));
        ecs.TransformEach<TestComp>((id, c) => c); // mark
        Assert.True(ecs.Changed<TestComp>(e));
    }
    
    [Fact]
    public void Spawn_Assigns_FirstGeneration()
    {
        var ecs = new Engine.EcsWorld();
        var id = ecs.Spawn();
        Assert.True(ecs.GetGeneration(id) >= 1);
    }

    [Fact]
    public void Despawn_Increments_Generation()
    {
        var ecs = new Engine.EcsWorld();
        var id = ecs.Spawn();
        var g1 = ecs.GetGeneration(id);
        ecs.Despawn(id);
        var g2 = ecs.GetGeneration(id);
        Assert.NotEqual(g1, g2);
        // ID is reused
        var id2 = ecs.Spawn();
        Assert.Equal(id, id2);
        Assert.Equal(g2, ecs.GetGeneration(id2));
    }

    [Fact]
    public void GetRef_By_Id_Works_After_Spawn_But_Not_After_Remove()
    {
        var ecs = new Engine.EcsWorld();
        var id = ecs.Spawn();
        ecs.Add(id, new TestComp { A = 1 });
        ref var c = ref ecs.GetRef<TestComp>(id);
        c.A = 5;
        Assert.True(ecs.TryGet<TestComp>(id, out var v) && v.A == 5);
        ecs.Despawn(id);
        // component gone; GetRef should throw missing component
        Assert.Throws<KeyNotFoundException>(() => { ref var _ = ref ecs.GetRef<TestComp>(id); });
    }
}
