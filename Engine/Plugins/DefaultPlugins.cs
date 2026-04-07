namespace Engine;

/// <summary>
/// Aggregates the standard set of engine plugins: window, input, ECS, behaviors, ImGui, renderer, and WebView.
/// </summary>
/// <remarks>
/// Also registers a <see cref="Stage.First"/> system that calls <see cref="EcsWorld.BeginFrame"/>
/// to advance the frame tick and clear per-frame change tracking.
/// </remarks>
/// <seealso cref="App"/>
/// <seealso cref="IPlugin"/>
public sealed class DefaultPlugins : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.Plugins");

    /// <inheritdoc />
    public void Build(App app)
    {
        Logger.Info("DefaultPlugins: Loading standard engine plugin set...");

        app.AddPlugin(new AppWindowPlugin())
           .AddPlugin(new AppExitPlugin())
           .AddPlugin(new ExceptionsPlugin())
           .AddPlugin(new TimePlugin())
           .AddPlugin(new InputPlugin())
           .AddPlugin(new EcsPlugin())
           .AddPlugin(new BehaviorsPlugin())
           .AddPlugin(new SdlImGuiPlugin())
           .AddPlugin(new SdlRendererPlugin())
           .AddPlugin(new VulkanWebViewPlugin())
           .AddPlugin(new VulkanImGuiPlugin());

        app.AddSystem(Stage.First, new SystemDescriptor(world =>
            {
                world.Resource<EcsWorld>().BeginFrame();
            }, "DefaultPlugins.EcsBeginFrame")
            .Write<EcsWorld>());

        Logger.Info("DefaultPlugins: All standard plugins loaded successfully.");
    }
}
