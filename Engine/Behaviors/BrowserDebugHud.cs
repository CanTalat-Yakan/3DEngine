using ImGuiNET;

namespace Engine;

/// <summary>ImGui diagnostic overlay for the Ultralight browser subsystem.</summary>
[Behavior]
public struct BrowserDebugHud
{
    /// <summary>Draws browser debug info every render frame.</summary>
    [OnRender]
    public static void Draw(BehaviorContext ctx)
    {
        var b = ctx.World.TryResource<BrowserInstance>();
        if (b is null) return;

        ImGui.Begin("Browser Debug", ImGuiWindowFlags.NoSavedSettings);

        // ── View info ────────────────────────────────────────────────
        ImGui.Text($"View:       {b.Width}x{b.Height}");
        ImGui.Text($"Surface:    {(b.HasSurface ? "YES" : "NO")}");
        ImGui.Text($"RowBytes:   {b.SurfaceRowBytes}  (expected {b.Width * 4})");
        ImGui.Text($"Page Title: {b.DiagPageTitle ?? "(null)"}");

        ImGui.Separator();

        // ── Frame counters ───────────────────────────────────────────
        ImGui.Text($"Updates:    {b.DiagUpdateCount}");
        ImGui.Text($"Paints:     {b.DiagPaintCount}");
        ImGui.Text($"Uploads:    {b.DiagUploadCount}");
        ImGui.Text($"IsDirty:    {b.IsDirty}");
        ImGui.Text($"NeedsPaint: {b.NeedsPaintNow}");

        ImGui.Separator();

        // ── Pixel content ────────────────────────────────────────────
        var pct = b.DiagTotalPixels > 0
            ? (double)b.DiagNonZeroPixels / b.DiagTotalPixels * 100.0
            : 0;
        ImGui.Text($"Non-zero:   {b.DiagNonZeroPixels}/{b.DiagTotalPixels} ({pct:F1}%)");
        ImGui.Text($"First bytes: {b.DiagFirstBytes ?? "n/a"}");

        ImGui.Separator();

        // ── Resource files on disk ───────────────────────────────────
        ImGui.Text($"ICU data:   {(b.DiagIcuExists ? "OK" : "MISSING")}");
        ImGui.Text($"CA certs:   {(b.DiagCaCertExists ? "OK" : "MISSING")}");

        ImGui.Separator();

        // ── Warnings ─────────────────────────────────────────────────
        if (b.DiagUpdateCount > 60 && b.DiagPaintCount == 0)
            ImGui.TextColored(new System.Numerics.Vector4(1, 0.3f, 0.3f, 1),
                "WARNING: NeedsPaint never true!");

        if (b.DiagUpdateCount > 60 && b.DiagUploadCount == 0)
            ImGui.TextColored(new System.Numerics.Vector4(1, 0.3f, 0.3f, 1),
                "WARNING: No uploads after 60+ frames!");

        if (b.DiagUploadCount > 0 && b.DiagNonZeroPixels == 0)
            ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1),
                "WARNING: Pixels uploaded but all zero!");

        if (!b.DiagIcuExists)
            ImGui.TextColored(new System.Numerics.Vector4(1, 0.3f, 0.3f, 1),
                "CRITICAL: icudt67l.dat missing — text layout will fail!");

        if (string.IsNullOrEmpty(b.DiagPageTitle))
            ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1),
                "WARNING: Page title is empty — page may not have loaded.");

        ImGui.End();
    }
}

