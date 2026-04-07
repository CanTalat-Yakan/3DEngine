using Editor.Shell;
using FluentAssertions;
using Xunit;

namespace Engine.Tests.Editor;

[Trait("Category", "Unit")]
public class ShellRegistryTests
{
    [Fact]
    public void Version_Starts_At_Zero()
    {
        var registry = new ShellRegistry();

        registry.Version.Should().Be(0);
    }

    [Fact]
    public void Current_Returns_Empty_Descriptor_By_Default()
    {
        var registry = new ShellRegistry();

        registry.Current.Should().NotBeNull();
        registry.Current.Panels.Should().BeEmpty();
    }

    [Fact]
    public void Update_Bumps_Version()
    {
        var registry = new ShellRegistry();

        registry.Update(new ShellDescriptor());
        registry.Version.Should().Be(1);

        registry.Update(new ShellDescriptor());
        registry.Version.Should().Be(2);
    }

    [Fact]
    public void Update_Sets_Current_To_New_Descriptor()
    {
        var registry = new ShellRegistry();
        var desc = new ShellDescriptor
        {
            Panels = [new PanelDescriptor { Id = "test", Title = "Test Panel" }]
        };

        registry.Update(desc);

        registry.Current.Should().BeSameAs(desc);
        registry.Current.Panels.Should().HaveCount(1);
        registry.Current.Panels[0].Id.Should().Be("test");
    }

    [Fact]
    public void Update_Null_Throws_ArgumentNullException()
    {
        var registry = new ShellRegistry();

        var act = () => registry.Update(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("descriptor");
    }

    [Fact]
    public void Changed_Event_Fires_On_Update()
    {
        var registry = new ShellRegistry();
        int eventCount = 0;
        registry.Changed += () => eventCount++;

        registry.Update(new ShellDescriptor());

        eventCount.Should().Be(1);
    }

    [Fact]
    public void Changed_Event_Fires_Outside_Lock()
    {
        // Verify that subscribing handler can read Current without deadlock
        var registry = new ShellRegistry();
        ShellDescriptor? capturedDescriptor = null;
        registry.Changed += () => capturedDescriptor = registry.Current;

        var desc = new ShellDescriptor();
        registry.Update(desc);

        capturedDescriptor.Should().BeSameAs(desc);
    }

    [Fact]
    public void Concurrent_Updates_Do_Not_Corrupt_State()
    {
        var registry = new ShellRegistry();
        const int iterations = 100;

        Parallel.For(0, iterations, i =>
        {
            registry.Update(new ShellDescriptor
            {
                Metadata = new Dictionary<string, object> { ["i"] = i }
            });
        });

        registry.Version.Should().Be(iterations);
        registry.Current.Should().NotBeNull();
    }

    // ── PanelDescriptor defaults ────────────────────────────────────────

    [Fact]
    public void PanelDescriptor_Has_Sensible_Defaults()
    {
        var panel = new PanelDescriptor();

        panel.Id.Should().BeEmpty();
        panel.Title.Should().BeEmpty();
        panel.DefaultZone.Should().Be(DockZone.Left);
        panel.InitialSize.Should().BeApproximately(0.25f, 0.001f);
        panel.Closeable.Should().BeTrue();
        panel.Visible.Should().BeTrue();
        panel.Content.Should().BeNull();
        panel.Icon.Should().BeNull();
        panel.TabGroupId.Should().BeNull();
        panel.Route.Should().BeNull();
    }

    // ── ShellDescriptor defaults ────────────────────────────────────────

    [Fact]
    public void ShellDescriptor_Defaults_Are_Empty()
    {
        var desc = new ShellDescriptor();

        desc.Panels.Should().BeEmpty();
        desc.Metadata.Should().BeEmpty();
    }
}

