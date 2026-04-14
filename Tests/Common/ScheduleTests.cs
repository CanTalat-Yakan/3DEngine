using FluentAssertions;
using Xunit;

namespace Engine.Tests.Common;

[Trait("Category", "Unit")]
public class ScheduleTests
{
    // ── Registration & Execution ────────────────────────────────────────

    [Fact]
    public void AddSystem_And_RunStage_Executes_System()
    {
        var schedule = new Schedule();
        var world = new World();
        int called = 0;

        schedule.AddSystem(Stage.Update, _ => called++);
        schedule.RunStage(Stage.Update, world);

        called.Should().Be(1);
    }

    [Fact]
    public void Systems_Execute_In_Registration_Order_When_SingleThreaded()
    {
        var schedule = new Schedule();
        schedule.SetSingleThreaded(Stage.Update);
        var world = new World();
        var order = new List<int>();

        schedule.AddSystem(Stage.Update, _ => order.Add(1));
        schedule.AddSystem(Stage.Update, _ => order.Add(2));
        schedule.AddSystem(Stage.Update, _ => order.Add(3));

        schedule.RunStage(Stage.Update, world);

        order.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void RunCondition_Skips_System_When_False()
    {
        var schedule = new Schedule();
        var world = new World();
        int called = 0;

        schedule.AddSystem(Stage.Update, _ => called++, _ => false);
        schedule.RunStage(Stage.Update, world);

        called.Should().Be(0);
    }

    [Fact]
    public void RunCondition_Executes_System_When_True()
    {
        var schedule = new Schedule();
        var world = new World();
        int called = 0;

        schedule.AddSystem(Stage.Update, _ => called++, _ => true);
        schedule.RunStage(Stage.Update, world);

        called.Should().Be(1);
    }

    // ── SystemDescriptor ────────────────────────────────────────────────

    [Fact]
    public void AddSystem_Descriptor_Executes()
    {
        var schedule = new Schedule();
        var world = new World();
        int called = 0;

        var desc = new SystemDescriptor(_ => called++, "TestSystem")
            .Write<World>();
        schedule.AddSystem(Stage.Update, desc);
        schedule.RunStage(Stage.Update, world);

        called.Should().Be(1);
    }

    [Fact]
    public void SystemDescriptor_InfersName_From_Delegate()
    {
        static void MySystem(World w) { }

        var desc = new SystemDescriptor(MySystem);

        desc.Name.Should().Contain("MySystem");
    }

    [Fact]
    public void SystemDescriptor_RunIf_Sets_RunCondition()
    {
        var desc = new SystemDescriptor(_ => { }, "Test")
            .RunIf(_ => false);

        desc.RunCondition.Should().NotBeNull();
    }

    [Fact]
    public void SystemDescriptor_MainThreadOnly_Sets_Affinity()
    {
        var desc = new SystemDescriptor(_ => { }, "Test")
            .MainThreadOnly();

        desc.Affinity.Should().Be(ThreadAffinity.MainThread);
    }

    // ── Resource Access Metadata & Conflicts ────────────────────────────

    [Fact]
    public void SystemDescriptor_Read_Write_Track_Types()
    {
        var desc = new SystemDescriptor(_ => { }, "Test")
            .Read<string>()
            .Write<int>();

        desc.Reads.Should().Contain(typeof(string));
        desc.Writes.Should().Contain(typeof(int));
        desc.HasExplicitAccess.Should().BeTrue();
    }

    [Fact]
    public void ConflictsWith_WriteWrite_Returns_True()
    {
        var a = new SystemDescriptor(_ => { }, "A").Write<int>();
        var b = new SystemDescriptor(_ => { }, "B").Write<int>();

        a.ConflictsWith(b).Should().BeTrue();
    }

    [Fact]
    public void ConflictsWith_ReadRead_Returns_False()
    {
        var a = new SystemDescriptor(_ => { }, "A").Read<int>();
        var b = new SystemDescriptor(_ => { }, "B").Read<int>();

        a.ConflictsWith(b).Should().BeFalse();
    }

    [Fact]
    public void ConflictsWith_WriteRead_Returns_True()
    {
        var a = new SystemDescriptor(_ => { }, "A").Write<int>();
        var b = new SystemDescriptor(_ => { }, "B").Read<int>();

        a.ConflictsWith(b).Should().BeTrue();
    }

    [Fact]
    public void ConflictsWith_NoMetadata_Is_Conservative()
    {
        var a = new SystemDescriptor(_ => { }, "A");
        var b = new SystemDescriptor(_ => { }, "B").Read<int>();

        a.ConflictsWith(b).Should().BeTrue();
    }

    [Fact]
    public void ConflictsWith_DisjointResources_Returns_False()
    {
        var a = new SystemDescriptor(_ => { }, "A").Write<int>();
        var b = new SystemDescriptor(_ => { }, "B").Write<string>();

        a.ConflictsWith(b).Should().BeFalse();
    }

    [Fact]
    public void TryGetConflictReason_Reports_WriteWrite()
    {
        var a = new SystemDescriptor(_ => { }, "A").Write<int>();
        var b = new SystemDescriptor(_ => { }, "B").Write<int>();

        a.TryGetConflictReason(b, out var reason).Should().BeTrue();
        reason.Should().Contain("write/write");
    }

    // ── RemoveSystems ───────────────────────────────────────────────────

    [Fact]
    public void RemoveSystems_Removes_Matching_Systems()
    {
        var schedule = new Schedule();
        schedule.AddSystem(Stage.Update, new SystemDescriptor(_ => { }, "Keep"));
        schedule.AddSystem(Stage.Update, new SystemDescriptor(_ => { }, "Remove"));

        int removed = schedule.RemoveSystems(Stage.Update, desc => desc.Name == "Remove");

        removed.Should().Be(1);
        schedule.SystemCount(Stage.Update).Should().Be(1);
    }

    // ── SystemCount / TotalSystemCount ──────────────────────────────────

    [Fact]
    public void SystemCount_Tracks_Per_Stage_Count()
    {
        var schedule = new Schedule();
        schedule.AddSystem(Stage.Update, _ => { });
        schedule.AddSystem(Stage.Update, _ => { });
        schedule.AddSystem(Stage.Render, _ => { });

        schedule.SystemCount(Stage.Update).Should().Be(2);
        schedule.SystemCount(Stage.Render).Should().Be(1);
        schedule.TotalSystemCount.Should().Be(3);
    }

    // ── Parallelism control ─────────────────────────────────────────────

    [Fact]
    public void SetParallel_And_SetSingleThreaded_Toggle()
    {
        var schedule = new Schedule();
        var world = new World();
        var order = new List<int>();

        schedule.SetSingleThreaded(Stage.Update);
        schedule.AddSystem(Stage.Update, _ => order.Add(1));
        schedule.AddSystem(Stage.Update, _ => order.Add(2));

        schedule.RunStage(Stage.Update, world);

        // Single-threaded should preserve order
        order.Should().Equal(1, 2);
    }

    // ── Exception isolation ─────────────────────────────────────────────

    [Fact]
    public void Faulting_System_Does_Not_Block_Subsequent_Systems()
    {
        var schedule = new Schedule();
        schedule.SetSingleThreaded(Stage.Update);
        var world = new World();
        int afterFault = 0;

        schedule.AddSystem(Stage.Update, _ => throw new InvalidOperationException("boom"));
        schedule.AddSystem(Stage.Update, _ => afterFault++);

        // Should not throw - exceptions are isolated
        var act = () => schedule.RunStage(Stage.Update, world);
        act.Should().NotThrow();

        afterFault.Should().Be(1);
    }

    // ── ScheduleDiagnostics ─────────────────────────────────────────────

    [Fact]
    public void Diagnostics_Records_Stage_And_System_Durations()
    {
        var schedule = new Schedule();
        var world = new World();

        schedule.AddSystem(Stage.Update, new SystemDescriptor(_ => Thread.Sleep(5), "SlowSystem"));
        schedule.RunStage(Stage.Update, world);

        schedule.Diagnostics.GetStageDuration(Stage.Update).Should().BeGreaterThan(TimeSpan.Zero);
        schedule.Diagnostics.GetSystemDuration(Stage.Update, "SlowSystem").Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void Diagnostics_StageDurations_Returns_Snapshot()
    {
        var schedule = new Schedule();
        var world = new World();

        schedule.AddSystem(Stage.Update, _ => { });
        schedule.RunStage(Stage.Update, world);

        var durations = schedule.Diagnostics.StageDurations;
        durations.Should().ContainKey(Stage.Update);
    }

    [Fact]
    public void Diagnostics_Reset_Clears_All()
    {
        var diag = new ScheduleDiagnostics();
        diag.RecordStage(Stage.Update, TimeSpan.FromMilliseconds(10));

        diag.Reset();

        diag.GetStageDuration(Stage.Update).Should().Be(TimeSpan.Zero);
    }

    // ── Empty stage is no-op ────────────────────────────────────────────

    [Fact]
    public void RunStage_With_No_Systems_Is_NoOp()
    {
        var schedule = new Schedule();
        var world = new World();

        var act = () => schedule.RunStage(Stage.Update, world);
        act.Should().NotThrow();
    }

    // ── Fine-grained per-component-type scheduling ───────────────────────

    /// <summary>Marker types used as resource keys to simulate per-behavior-type writes.</summary>
    private struct CompA;
    private struct CompB;
    private struct CompC;

    [Fact]
    public void DisjointComponentWrites_Are_Batched_Together()
    {
        // Two systems writing different component types should NOT conflict,
        // mirroring the generator's per-behavior-type Write<T>() metadata.
        var a = new SystemDescriptor(_ => { }, "SystemA").Write<CompA>();
        var b = new SystemDescriptor(_ => { }, "SystemB").Write<CompB>();

        a.ConflictsWith(b).Should().BeFalse("different component-type writes should not conflict");
    }

    [Fact]
    public void SameComponentWrite_Conflicts()
    {
        // Two instance behaviors of the same type would conflict (same Write<T>).
        var a = new SystemDescriptor(_ => { }, "SystemA").Write<CompA>();
        var b = new SystemDescriptor(_ => { }, "SystemB").Write<CompA>();

        a.ConflictsWith(b).Should().BeTrue("same component-type writes must conflict");
    }

    [Fact]
    public void ReadEcsWorld_Does_Not_Conflict_With_ComponentWrite()
    {
        // A static behavior with Read<EcsWorld>() should not conflict with
        // an instance behavior with Write<CompA>() - different resource types.
        var staticSys = new SystemDescriptor(_ => { }, "StaticSys").Read<EcsWorld>();
        var instanceSys = new SystemDescriptor(_ => { }, "InstanceSys").Write<CompA>();

        staticSys.ConflictsWith(instanceSys).Should().BeFalse(
            "a reader of EcsWorld should not conflict with a writer of a different component type");
    }

    [Fact]
    public void MultipleReadEcsWorld_Do_Not_Conflict()
    {
        // Multiple static behaviors with Read<EcsWorld>() should all be parallel.
        var a = new SystemDescriptor(_ => { }, "StaticA").Read<EcsWorld>();
        var b = new SystemDescriptor(_ => { }, "StaticB").Read<EcsWorld>();

        a.ConflictsWith(b).Should().BeFalse("read/read on EcsWorld should not conflict");
    }

    [Fact]
    public void Parallel_Batching_Groups_DisjointWrites()
    {
        var schedule = new Schedule();
        var world = new World();
        int count = 0;

        // Three systems writing to three different types - should all land in one batch.
        schedule.AddSystem(Stage.Update, new SystemDescriptor(_ => Interlocked.Increment(ref count), "SysA").Write<CompA>());
        schedule.AddSystem(Stage.Update, new SystemDescriptor(_ => Interlocked.Increment(ref count), "SysB").Write<CompB>());
        schedule.AddSystem(Stage.Update, new SystemDescriptor(_ => Interlocked.Increment(ref count), "SysC").Write<CompC>());

        schedule.RunStage(Stage.Update, world);

        count.Should().Be(3, "all systems should execute");

        // Verify batching: with disjoint writes, they should be in a single batch.
        var batches = schedule.Diagnostics.StageBatches;
        batches.Should().ContainKey(Stage.Update);
        batches[Stage.Update].Should().HaveCount(1, "disjoint-write systems should be packed into one parallel batch");
        batches[Stage.Update][0].Should().HaveCount(3);
    }

    [Fact]
    public void Parallel_Batching_Splits_ConflictingWrites()
    {
        var schedule = new Schedule();
        var world = new World();

        // Two systems writing the same type - must be in separate batches.
        schedule.AddSystem(Stage.Update, new SystemDescriptor(_ => { }, "SysA1").Write<CompA>());
        schedule.AddSystem(Stage.Update, new SystemDescriptor(_ => { }, "SysA2").Write<CompA>());

        schedule.RunStage(Stage.Update, world);

        var batches = schedule.Diagnostics.StageBatches;
        batches[Stage.Update].Should().HaveCount(2, "conflicting-write systems must be in separate batches");
    }
}

