using ImGuiNET;

namespace Engine;

/// <summary>ImGui diagnostic overlay for the Ultralight browser subsystem.</summary>
// [Behavior]
public struct BrowserDebugHud
{
    private static readonly System.Numerics.Vector4 Red = new(1, 0.3f, 0.3f, 1);
    private static readonly System.Numerics.Vector4 Yellow = new(1, 1, 0, 1);
    private static readonly System.Numerics.Vector4 Green = new(0.3f, 1, 0.3f, 1);
    private static string? _dumpPath;

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

        // ── Page load state (from native callbacks) ──────────────────
        ImGui.TextColored(b.DiagDOMReady ? Green : Yellow,
            $"DOM Ready:  {b.DiagDOMReady}");
        ImGui.TextColored(b.DiagPageFinished ? Green : Yellow,
            $"Page Done:  {b.DiagPageFinished}");
        if (b.DiagLoadError is not null)
            ImGui.TextColored(Red, $"Load Error: {b.DiagLoadError}");
        if (b.DiagLastConsoleMessage is not null)
            ImGui.Text($"Console:    {b.DiagLastConsoleMessage}");

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
        ImGui.Text($"Non-zero (upload): {b.DiagNonZeroPixels}/{b.DiagTotalPixels} ({pct:F1}%)");

        var probePct = b.DiagTotalPixels > 0
            ? (double)b.DiagProbeNonZero / b.DiagTotalPixels * 100.0
            : 0;
        ImGui.Text($"Non-zero (probe):  {b.DiagProbeNonZero}/{b.DiagTotalPixels} ({probePct:F1}%)");
        ImGui.Text($"First bytes: {b.DiagFirstBytes ?? "n/a"}");

        ImGui.Separator();

        // ── Resource files on disk ───────────────────────────────────
        ImGui.Text($"ICU data:   {(b.DiagIcuExists ? "OK" : "MISSING")}");
        ImGui.Text($"CA certs:   {(b.DiagCaCertExists ? "OK" : "MISSING")}");

        ImGui.Separator();

        // ── Warnings ─────────────────────────────────────────────────
        if (!b.DiagDOMReady && b.DiagUpdateCount > 60)
            ImGui.TextColored(Red,
                "CRITICAL: DOM never became ready — page did not load!");

        if (b.DiagLoadError is not null)
            ImGui.TextColored(Red,
                "CRITICAL: Page load failed! Check log for details.");

        if (b.DiagUpdateCount > 60 && b.DiagPaintCount == 0)
            ImGui.TextColored(Red,
                "WARNING: NeedsPaint never true!");

        if (b.DiagUpdateCount > 60 && b.DiagUploadCount == 0)
            ImGui.TextColored(Red,
                "WARNING: No uploads after 60+ frames!");

        if (b.DiagUploadCount > 0 && b.DiagNonZeroPixels == 0 && b.DiagProbeNonZero == 0)
            ImGui.TextColored(Yellow,
                "WARNING: Pixels uploaded but all zero (upload + probe)!");

        if (b.DiagUploadCount > 0 && b.DiagNonZeroPixels == 0 && b.DiagProbeNonZero > 0)
            ImGui.TextColored(Yellow,
                "HINT: Probe found pixels but upload didn't — timing issue!");

        if (!b.DiagIcuExists)
            ImGui.TextColored(Red,
                "CRITICAL: icudt67l.dat missing — text layout will fail!");

        ImGui.Separator();

        // ── Dump to file ─────────────────────────────────────────────
        if (ImGui.Button("Dump to File"))
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Browser Debug ===");
            sb.AppendLine($"View:       {b.Width}x{b.Height}");
            sb.AppendLine($"Surface:    {(b.HasSurface ? "YES" : "NO")}");
            sb.AppendLine($"RowBytes:   {b.SurfaceRowBytes}  (expected {b.Width * 4})");
            sb.AppendLine($"Page Title: {b.DiagPageTitle ?? "(null)"}");
            sb.AppendLine($"DOM Ready:  {b.DiagDOMReady}");
            sb.AppendLine($"Page Done:  {b.DiagPageFinished}");
            if (b.DiagLoadError is not null)
                sb.AppendLine($"Load Error: {b.DiagLoadError}");
            if (b.DiagLastConsoleMessage is not null)
                sb.AppendLine($"Console:    {b.DiagLastConsoleMessage}");
            sb.AppendLine($"Updates:    {b.DiagUpdateCount}");
            sb.AppendLine($"Paints:     {b.DiagPaintCount}");
            sb.AppendLine($"Uploads:    {b.DiagUploadCount}");
            sb.AppendLine($"IsDirty:    {b.IsDirty}");
            sb.AppendLine($"NeedsPaint: {b.NeedsPaintNow}");
            sb.AppendLine($"Non-zero (upload): {b.DiagNonZeroPixels}/{b.DiagTotalPixels}");
            sb.AppendLine($"Non-zero (probe):  {b.DiagProbeNonZero}/{b.DiagTotalPixels}");
            sb.AppendLine($"First bytes: {b.DiagFirstBytes ?? "n/a"}");
            sb.AppendLine($"ICU data:   {(b.DiagIcuExists ? "OK" : "MISSING")}");
            sb.AppendLine($"CA certs:   {(b.DiagCaCertExists ? "OK" : "MISSING")}");

            // Warnings
            if (!b.DiagDOMReady && b.DiagUpdateCount > 60)
                sb.AppendLine("CRITICAL: DOM never became ready — page did not load!");
            if (b.DiagLoadError is not null)
                sb.AppendLine("CRITICAL: Page load failed!");
            if (b.DiagUpdateCount > 60 && b.DiagPaintCount == 0)
                sb.AppendLine("WARNING: NeedsPaint never true!");
            if (b.DiagUpdateCount > 60 && b.DiagUploadCount == 0)
                sb.AppendLine("WARNING: No uploads after 60+ frames!");
            if (b.DiagUploadCount > 0 && b.DiagNonZeroPixels == 0 && b.DiagProbeNonZero == 0)
                sb.AppendLine("WARNING: Pixels uploaded but all zero (upload + probe)!");
            if (b.DiagUploadCount > 0 && b.DiagNonZeroPixels == 0 && b.DiagProbeNonZero > 0)
                sb.AppendLine("HINT: Probe found pixels but upload didn't — timing issue!");
            if (!b.DiagIcuExists)
                sb.AppendLine("CRITICAL: icudt67l.dat missing — text layout will fail!");

            var path = Path.Combine(AppContext.BaseDirectory, "browser_debug.txt");
            File.WriteAllText(path, sb.ToString());
            _dumpPath = path;
        }

        if (_dumpPath is not null)
            ImGui.Text($"Saved: {_dumpPath}");

        ImGui.End();
    }
}

