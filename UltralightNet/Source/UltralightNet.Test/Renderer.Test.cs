using System.Collections.Generic;

namespace UltralightNet.Test;

[Collection("Renderer")]
[Trait("Category", "Renderer")]
public sealed class RendererTest(RendererFixture fixture)
{
	private Renderer Renderer { get; } = fixture.Renderer;

	[Fact]
	public void SessionTest()
	{
		var session = Renderer.DefaultSession;
		Assert.Equal("default", session.Name);
		Assert.Equal(OperatingSystem.IsWindows() ? "default" : "/default", session.DiskPath);

		using var session1 = Renderer.CreateSession(false, "myses1");
		Assert.Equal("myses1", session1.Name);
		Assert.False(session1.IsPersistent);

		using var session2 = Renderer.CreateSession(true, "myses2");
		Assert.Equal("myses2", session2.Name);
		Assert.True(session2.IsPersistent);

		Assert.True(session.Id != session1.Id && session1.Id != session2.Id);

		Assert.NotEqual(session, session1);
		Assert.Equal(session1, session1);

		Assert.Throws<NotSupportedException>(() => new Dictionary<Session, object>(1) { [session] = new() });
	}

	[Fact]
	public void InspectorTest()
	{
		Assert.True(Renderer.TryStartRemoteInspectorServer("127.0.0.1", 7676));
	}

	[Fact]
	public void MemoryTest()
	{
		Renderer.LogMemoryUsage();
		Renderer.PurgeMemory();
		Renderer.LogMemoryUsage();
	}

	[Fact]
	public void GpuDriverNotSet()
	{
		Assert.Throws<Exception>(() => Renderer.CreateView(128, 128, new ViewConfig { IsAccelerated = true }));
	}
}
