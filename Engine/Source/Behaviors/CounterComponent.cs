using ImGuiNET;

namespace Engine;

/// <summary>Behavior that increments a counter each update and displays its value.</summary>
[Behavior]
public struct CounterComponent
{
    private static int _count;

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        _count++;
    }

    [OnRender]
    public static void Draw(BehaviorContext ctx)
    {
        ImGui.Begin("HUD");
        ImGui.Text($"Count: {_count}");
        ImGui.Text($"Entities: {ctx.Ecs.Count<CounterComponent>()}");
        ImGui.End();
    }
}

