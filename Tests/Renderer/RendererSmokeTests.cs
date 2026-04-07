using FluentAssertions;
using Xunit;

namespace Engine.Tests.Renderer;

[Trait("Category", "Smoke")]
public class RendererSmokeTests
{
    private sealed class StubSurfaceSource : ISurfaceSource
    {
        public IReadOnlyList<string> GetRequiredInstanceExtensions() => Array.Empty<string>();
        public nint CreateSurfaceHandle(nint instanceHandle) => 0;
        public (uint Width, uint Height) GetDrawableSize() => (800, 600);
    }

    [Fact]
    public void Renderer_Can_Render_Frame_With_NullGraphics()
    {
        var nullGfx = new NullGraphicsDevice();
        var context = new RendererContext(nullGfx);
        context.Initialize(new StubSurfaceSource());
        var renderer = new Engine.Renderer(context);

        renderer.Initialize();
        var act = () => renderer.RenderFrame(new World());

        act.Should().NotThrow();
    }
}

