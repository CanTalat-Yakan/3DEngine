using ImGuiNET;

namespace Engine;

/// <summary>ImGui diagnostic overlay for the Ultralight webview subsystem.</summary>
/// <remarks>
/// Toggled with <c>Alt+F3</c> (default off).  Displays view dimensions, surface state,
/// page load callbacks, frame counters, resource file status, and actionable warnings
/// when common failure conditions are detected.
/// </remarks>
/// <seealso cref="WebViewInstance"/>
/// <seealso cref="WebViewRenderNode"/>
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

        ImGui.Separator();

        // ── Resource files on disk ───────────────────────────────────
        ImGui.Text($"ICU data:   {(w.DiagIcuExists ? "OK" : "MISSING")}");
        ImGui.Text($"CA certs:   {(w.DiagCaCertExists ? "OK" : "MISSING")}");

        ImGui.Separator();

        // ── Warnings ─────────────────────────────────────────────────
        if (!w.DiagDOMReady && w.DiagUpdateCount > 60)
            ImGui.TextColored(Red,
                "CRITICAL: DOM never became ready - page did not load!");

        if (w.DiagLoadError is not null)
            ImGui.TextColored(Red,
                "CRITICAL: Page load failed! Check log for details.");

        if (w.DiagUpdateCount > 60 && w.DiagUploadCount == 0)
            ImGui.TextColored(Red,
                "WARNING: No uploads after 60+ frames!");

        if (!w.DiagIcuExists)
            ImGui.TextColored(Red,
                "CRITICAL: icudt67l.dat missing - text layout will fail!");

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
            sb.AppendLine($"ICU data:   {(w.DiagIcuExists ? "OK" : "MISSING")}");
            sb.AppendLine($"CA certs:   {(w.DiagCaCertExists ? "OK" : "MISSING")}");

            // Warnings
            if (!w.DiagDOMReady && w.DiagUpdateCount > 60)
                sb.AppendLine("CRITICAL: DOM never became ready - page did not load!");
            if (w.DiagLoadError is not null)
                sb.AppendLine("CRITICAL: Page load failed!");
            if (w.DiagUpdateCount > 60 && w.DiagUploadCount == 0)
                sb.AppendLine("WARNING: No uploads after 60+ frames!");
            if (!w.DiagIcuExists)
                sb.AppendLine("CRITICAL: icudt67l.dat missing - text layout will fail!");

            var path = Path.Combine(AppContext.BaseDirectory, "webview_debug.txt");
            File.WriteAllText(path, sb.ToString());
            _dumpPath = path;
        }

        if (_dumpPath is not null)
            ImGui.Text($"Saved: {_dumpPath}");

        ImGui.End();
    }

    /// <summary>Logs a concise diagnostic snapshot of the webview state to the engine logger.</summary>
    private static void LogSnapshot(WebViewInstance w)
    {
        Logger.Info(
            $"WebViewDebug view={w.Width}x{w.Height} " +
            $"updates={w.DiagUpdateCount} paints={w.DiagPaintCount} uploads={w.DiagUploadCount}");

        if (w.DiagLoadError is not null)
            Logger.Warn($"WebViewDebug load error: {w.DiagLoadError}");
    }
}

