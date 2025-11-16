using System;
using Engine;
using Xunit;

namespace Engine.Tests;

public class DescriptorTests
{
    [Fact]
    public void NullGraphicsDevice_Does_Not_Support_Descriptors()
    {
        var nullGfx = new NullGraphicsDevice();
        Assert.Throws<NotSupportedException>(() => nullGfx.CreateDescriptorSet());
    }
}
