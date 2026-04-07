using ImGuiNET;

namespace Engine;

/// <summary>ImGui diagnostic overlay for the Ultralight webview subsystem.</summary>
[Behavior]
public struct WebViewDebugHud
{
    private static readonly ILogger Logger = Log.Category("Engine.WebView.DebugHud");
    private static readonly System.Numerics.Vector4 Red = new(1, 0.3f, 0.3f, 1);
    private static readonly System.Numerics.Vector4 Yellow = new(1, 1, 0, 1);
    private static readonly System.Numerics.Vector4 Green = new(0.3f, 1, 0.3f, 1);
    private static string? _dumpPath;

    /// <summary>Draws webview debug info every render frame.</summary>
    [OnRender]
    [ToggleKey(Key.F3, KeyModifier.Alt, DefaultEnabled = false)]
    public static void Draw(BehaviorContext ctx)
    {
        if (!ctx.World.TryGetResource<WebViewInstance>(out var w)) return;

        ImGui.Begin("WebView Debug", ImGuiWindowFlags.NoSavedSettings);

        // ── View info ────────────────────────────────────────────────
        ImGui.Text($"View:       {w.Width}x{w.Height}");
        ImGui.Text($"Surface:    {(w.HasSurface ? "YES" : "NO")}");
        ImGui.Text($"RowBytes:   {w.SurfaceRowBytes}  (expected {w.Width * 4})");
        ImGui.Text($"Page Title: {w.DiagPageTitle ?? "(null)"}");

        ImGui.Separator();

        // ── Page load state (from native callbacks) ──────────────────
        ImGui.TextColored(w.DiagDOMReady ? Green : Yellow,
            $"DOM Ready:  {w.DiagDOMReady}");
        ImGui.TextColored(w.DiagPageFinished ? Green : Yellow,
            $"Page Done:  {w.DiagPageFinished}");
        if (w.DiagLoadError is not null)
            ImGui.TextColored(Red, $"Load Error: {w.DiagLoadError}");
        if (w.DiagLastConsoleMessage is not null)
            ImGui.Text($"Console:    {w.DiagLastConsoleMessage}");

        ImGui.Separator();

        // ── Frame counters ───────────────────────────────────────────
        ImGui.Text($"Updates:    {w.DiagUpdateCount}");
        ImGui.Text($"Paints:     {w.DiagPaintCount}");
        ImGui.Text($"Uploads:    {w.DiagUploadCount}");
        ImGui.Text($"IsDirty:    {w.IsDirty}");
        ImGui.Text($"NeedsPaint: {w.NeedsPaintNow}");

        ImGui.Separator();

        // ── Pixel content ────────────────────────────────────────────
        var pct = w.DiagTotalPixels > 0
            ? (double)w.DiagNonZeroPixels / w.DiagTotalPixels * 100.0
            : 0;
        ImGui.Text($"Non-zero (upload): {w.DiagNonZeroPixels}/{w.DiagTotalPixels} ({pct:F1}%)");

        var probePct = w.DiagTotalPixels > 0
            ? (double)w.DiagProbeNonZero / w.DiagTotalPixels * 100.0
            : 0;
        ImGui.Text($"Non-zero (probe):  {w.DiagProbeNonZero}/{w.DiagTotalPixels} ({probePct:F1}%)");
        ImGui.Text($"First bytes: {w.DiagFirstBytes ?? "n/a"}");

        ImGui.Separator();

        // ── Resource files on disk ───────────────────────────────────
        ImGui.Text($"ICU data:   {(w.DiagIcuExists ? "OK" : "MISSING")}");
        ImGui.Text($"CA certs:   {(w.DiagCaCertExists ? "OK" : "MISSING")}");

        ImGui.Separator();

        // ── Warnings ─────────────────────────────────────────────────
        if (!w.DiagDOMReady && w.DiagUpdateCount > 60)
            ImGui.TextColored(Red,
                "CRITICAL: DOM never became ready — page did not load!");

        if (w.DiagLoadError is not null)
            ImGui.TextColored(Red,
                "CRITICAL: Page load failed! Check log for details.");

        if (w.DiagUpdateCount > 60 && w.DiagPaintCount == 0)
            ImGui.TextColored(Red,
                "WARNING: NeedsPaint never true!");

        if (w.DiagUpdateCount > 60 && w.DiagUploadCount == 0)
            ImGui.TextColored(Red,
                "WARNING: No uploads after 60+ frames!");

        if (w.DiagUploadCount > 0 && w.DiagNonZeroPixels == 0 && w.DiagProbeNonZero == 0)
            ImGui.TextColored(Yellow,
                "WARNING: Pixels uploaded but all zero (upload + probe)!");

        if (w.DiagUploadCount > 0 && w.DiagNonZeroPixels == 0 && w.DiagProbeNonZero > 0)
            ImGui.TextColored(Yellow,
                "HINT: Probe found pixels but upload didn't — timing issue!");

        if (!w.DiagIcuExists)
            ImGui.TextColored(Red,
                "CRITICAL: icudt67l.dat missing — text layout will fail!");

        ImGui.Separator();

        // ── Log / Dump ───────────────────────────────────────────────
        if (ImGui.Button("Log Snapshot"))
            LogSnapshot(w);

        ImGui.SameLine();

        if (ImGui.Button("Dump to File"))
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== WebView Debug ===");
            sb.AppendLine($"View:       {w.Width}x{w.Height}");
            sb.AppendLine($"Surface:    {(w.HasSurface ? "YES" : "NO")}");
            sb.AppendLine($"RowBytes:   {w.SurfaceRowBytes}  (expected {w.Width * 4})");
            sb.AppendLine($"Page Title: {w.DiagPageTitle ?? "(null)"}");
            sb.AppendLine($"DOM Ready:  {w.DiagDOMReady}");
            sb.AppendLine($"Page Done:  {w.DiagPageFinished}");
            if (w.DiagLoadError is not null)
                sb.AppendLine($"Load Error: {w.DiagLoadError}");
            if (w.DiagLastConsoleMessage is not null)
                sb.AppendLine($"Console:    {w.DiagLastConsoleMessage}");
            sb.AppendLine($"Updates:    {w.DiagUpdateCount}");
            sb.AppendLine($"Paints:     {w.DiagPaintCount}");
            sb.AppendLine($"Uploads:    {w.DiagUploadCount}");
            sb.AppendLine($"IsDirty:    {w.IsDirty}");
            sb.AppendLine($"NeedsPaint: {w.NeedsPaintNow}");
            sb.AppendLine($"Non-zero (upload): {w.DiagNonZeroPixels}/{w.DiagTotalPixels}");
            sb.AppendLine($"Non-zero (probe):  {w.DiagProbeNonZero}/{w.DiagTotalPixels}");
            sb.AppendLine($"First bytes: {w.DiagFirstBytes ?? "n/a"}");
            sb.AppendLine($"ICU data:   {(w.DiagIcuExists ? "OK" : "MISSING")}");
            sb.AppendLine($"CA certs:   {(w.DiagCaCertExists ? "OK" : "MISSING")}");

            // Warnings
            if (!w.DiagDOMReady && w.DiagUpdateCount > 60)
                sb.AppendLine("CRITICAL: DOM never became ready — page did not load!");
            if (w.DiagLoadError is not null)
                sb.AppendLine("CRITICAL: Page load failed!");
            if (w.DiagUpdateCount > 60 && w.DiagPaintCount == 0)
                sb.AppendLine("WARNING: NeedsPaint never true!");
            if (w.DiagUpdateCount > 60 && w.DiagUploadCount == 0)
                sb.AppendLine("WARNING: No uploads after 60+ frames!");
            if (w.DiagUploadCount > 0 && w.DiagNonZeroPixels == 0 && w.DiagProbeNonZero == 0)
                sb.AppendLine("WARNING: Pixels uploaded but all zero (upload + probe)!");
            if (w.DiagUploadCount > 0 && w.DiagNonZeroPixels == 0 && w.DiagProbeNonZero > 0)
                sb.AppendLine("HINT: Probe found pixels but upload didn't — timing issue!");
            if (!w.DiagIcuExists)
                sb.AppendLine("CRITICAL: icudt67l.dat missing — text layout will fail!");

            var path = Path.Combine(AppContext.BaseDirectory, "webview_debug.txt");
            File.WriteAllText(path, sb.ToString());
            _dumpPath = path;
        }

        if (_dumpPath is not null)
            ImGui.Text($"Saved: {_dumpPath}");

        ImGui.End();
    }

    private static void LogSnapshot(WebViewInstance w)
    {
        var pct = w.DiagTotalPixels > 0
            ? (double)w.DiagNonZeroPixels / w.DiagTotalPixels * 100.0
            : 0;

        Logger.Info(
            $"WebViewDebug view={w.Width}x{w.Height} dirty={w.IsDirty} needsPaint={w.NeedsPaintNow} " +
            $"updates={w.DiagUpdateCount} paints={w.DiagPaintCount} uploads={w.DiagUploadCount} " +
            $"nonZero={w.DiagNonZeroPixels}/{w.DiagTotalPixels} ({pct:F1}%)");

        if (w.DiagLoadError is not null)
            Logger.Warn($"WebViewDebug load error: {w.DiagLoadError}");
    }
}

