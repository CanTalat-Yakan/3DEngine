using FluentAssertions;
using Xunit;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class RenderDrawListsTests
{
    [Fact]
    public void Clear_Empties_Both_Lists()
    {
        var lists = new RenderDrawLists();
        lists.Opaque.Add(new DrawCommand(1, 0));
        lists.Transparent.Add(new DrawCommand(2, 100));

        lists.Clear();

        lists.Opaque.Should().BeEmpty();
        lists.Transparent.Should().BeEmpty();
    }

    [Fact]
    public void Lists_Start_Empty()
    {
        var lists = new RenderDrawLists();

        lists.Opaque.Should().BeEmpty();
        lists.Transparent.Should().BeEmpty();
    }

    [Fact]
    public void Opaque_And_Transparent_Are_Independent()
    {
        var lists = new RenderDrawLists();

        lists.Opaque.Add(new DrawCommand(1, 0));
        lists.Opaque.Add(new DrawCommand(2, 1));
        lists.Transparent.Add(new DrawCommand(3, 100));

        lists.Opaque.Should().HaveCount(2);
        lists.Transparent.Should().HaveCount(1);
    }
}

