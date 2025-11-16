using Xunit;

namespace Engine.Tests;

public class RendererSmokeTests
{
    [Fact]
    public void Renderer_Can_Render_Frame_With_NullGraphics()
    {
        var nullGfx = new NullGraphicsDevice();
        var context = new RendererContext(nullGfx);
        var renderer = new Renderer(context);
        renderer.Initialize();
        renderer.RenderFrame(new object());
        Assert.True(true);
    }
}
