using ImGuiNET;

namespace Engine;

/// <summary>Plugin contract for extending the application (insert resources, add systems, etc.).</summary>
public interface IPlugin
{
    /// <summary>Called once during app setup to configure the app/world.</summary>
    void Build(App app);
}

/// <summary>Central application object holding the World and execution Schedule; supports plugin composition.</summary>
public sealed class App
{
    /// <summary>Shared global state and resources.</summary>
    public World World { get; } = new();
    /// <summary>Holds systems grouped by stage and runs them on demand.</summary>
    public Schedule Schedule { get; } = new();

    private readonly List<IPlugin> _plugins = new();

    public App(Config? config = null)
    {
        World.InsertResource(config ?? Config.GetDefault());
    }

    /// <summary>Adds and builds a single plugin.</summary>
    public App AddPlugin(IPlugin plugin)
    {
        _plugins.Add(plugin);
        plugin.Build(this);
        return this;
    }

    /// <summary>Adds a plugin that itself registers multiple plugins (convention).</summary>
    public App AddPlugins(IPlugin pluginGroup) => AddPlugin(pluginGroup);

    /// <summary>Registers a system to a given stage.</summary>
    public App AddSystem(Stage stage, SystemFn system)
    {
        Schedule.AddSystem(stage, system);
        return this;
    }

    /// <summary>Inserts or replaces a world resource value.</summary>
    public App InsertResource<T>(T value) where T : notnull
    {
        World.InsertResource(value);
        return this;
    }

    /// <summary>Runs Startup once, then per-frame stages in the window loop; cleans up ImGui and window on exit.</summary>
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

        try
        {
            if (World.TryResource<ImGuiRenderer>() is { } imguiRenderer)
            {
                imguiRenderer.Dispose();
                World.RemoveResource<ImGuiRenderer>();
            }
            ImGui.DestroyContext();
        }
        catch { }

        appWindow.Dispose(null);
    }
}
