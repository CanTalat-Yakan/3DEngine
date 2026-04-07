using FluentAssertions;
using Xunit;

namespace Engine.Tests.Common;

[Trait("Category", "Unit")]
public class StageOrderTests
{
    [Fact]
    public void AllInOrder_Returns_All_8_Stages_In_Correct_Order()
    {
        var all = StageOrder.AllInOrder().ToArray();

        all.Should().HaveCount(8);
        all.Should().Equal(
            Stage.Startup,
            Stage.First,
            Stage.PreUpdate,
            Stage.Update,
            Stage.PostUpdate,
            Stage.Render,
            Stage.Last,
            Stage.Cleanup);
    }

    [Fact]
    public void FrameStages_Returns_6_Stages_Without_Startup_And_Cleanup()
    {
        var frame = StageOrder.FrameStages().ToArray();

        frame.Should().HaveCount(6);
        frame.Should().NotContain(Stage.Startup);
        frame.Should().NotContain(Stage.Cleanup);
        frame.Should().Equal(
            Stage.First,
            Stage.PreUpdate,
            Stage.Update,
            Stage.PostUpdate,
            Stage.Render,
            Stage.Last);
    }

    [Fact]
    public void FrameStages_Is_Subset_Of_AllInOrder()
    {
        var all = StageOrder.AllInOrder().ToArray();
        var frame = StageOrder.FrameStages().ToArray();

        all.Should().ContainInConsecutiveOrder(frame);
    }
}

