using Xunit;

namespace Engine.Tests;

public static class DummyRegistration
{
    public static int Calls;

    [GeneratedBehaviorRegistration]
    public static void Register(App app)
    {
        Calls++;
    }
}

public class BehaviorRegistrationTests
{
    [Fact]
    public void BehaviorsPlugin_Invokes_Annotated_Registration_Methods()
    {
        var app = new App();
        new BehaviorsPlugin().Build(app);
        Assert.True(DummyRegistration.Calls >= 1);
    }
}

