using System.Numerics;
using ImGuiNET;
using SDL3;

namespace Engine;

public interface IPlugin
{
    void Build(App app);
}

/// <summary>
/// Bevy-like application builder: holds a World, a Schedule, plugins, and runs the main loop.
/// </summary>
public sealed class App
{
    public World World { get; } = new();
    public Schedule Schedule { get; } = new();

    private readonly List<IPlugin> _plugins = new();

    public App(Config? config = null)
    {
        World.InsertResource(config ?? Config.GetDefault(multiSample: MultiSample.x4, defaultBoot: true));
    }

    public App AddPlugin(IPlugin plugin)
    {
        _plugins.Add(plugin);
        plugin.Build(this);
        return this;
    }

    public App AddPlugins(IPlugin pluginGroup) => AddPlugin(pluginGroup);

    public App AddSystem(Stage stage, SystemFn system)
    {
        Schedule.AddSystem(stage, system);
        return this;
    }

    public App InsertResource<T>(T value) where T : notnull
    {
        World.InsertResource(value);
        return this;
    }

    /// <summary>
    /// Runs Startup stage once, then enters the per-frame loop driving the remaining stages.
    /// </summary>
    public void Run()
    {
        var window = World.TryResource<AppWindow>();
        if (window is null)
        {
            new WindowPlugin().Build(this);
            window = World.Resource<AppWindow>();
        }

        Schedule.RunStage(Stage.Startup, World);

        var appWindow = window;

        appWindow.Looping((Action)(() =>
        {
            Schedule.RunStage(Stage.First, World);
            Schedule.RunStage(Stage.PreUpdate, World);
            Schedule.RunStage(Stage.Update, World);
            Schedule.RunStage(Stage.PostUpdate, World);
            Schedule.RunStage(Stage.Render, World);
            Schedule.RunStage(Stage.Last, World);
        }));

        // Cleanup (best-effort)
        try
        {
            if (World.TryResource<GUIRenderer>() is { } imguiRenderer)
            {
                imguiRenderer.Dispose();
                World.RemoveResource<GUIRenderer>();
            }
            ImGui.DestroyContext();
        }
        catch { }

        try
        {
            if (World.TryResource<Kernel>() is { } kernel)
            {
                kernel.Dispose();
                World.RemoveResource<Kernel>();
            }
        }
        catch { }

        appWindow.Dispose(null);
    }
}

// Plugins moved to src/plugins
