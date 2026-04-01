using ImGuiNET;

namespace Engine;

/// <summary>Tracks a per-entity tick counter and displays entity statistics in the HUD.</summary>
[Behavior]
public struct EntityCounter
{
    public int Ticks;

    /// <summary>Increments this entity's tick counter each update.</summary>
    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Ticks++;
        ctx.Ecs.Update(ctx.EntityId, this);
    }

    /// <summary>Displays aggregate entity statistics.</summary>
    [OnRender]
    public static void Draw(BehaviorContext ctx)
    {
        int count = ctx.Ecs.Count<EntityCounter>();

        ImGui.Begin("Performance");
        ImGui.Text($"Entities:  {count:N0}");
        ImGui.End();
    }
}

