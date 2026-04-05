using System;
using Engine;
using Xunit;

namespace Engine.Tests;

public class BufferTests
{
    [Fact]
    public void NullGraphicsDevice_Does_Not_Support_Buffers()
    {
        var nullGfx = new NullGraphicsDevice();
        Assert.Throws<NotSupportedException>(() => nullGfx.CreateBuffer(new BufferDesc(16, BufferUsage.Vertex, CpuAccessMode.Write)));
    }

    // Vulkan-backed buffer tests will be added in a dedicated integration harness where a valid surface is available.
}
