using System.Diagnostics;
using Editor.Server;
using Editor.Shell;
using Engine;

// ── Editor Architecture ──────────────────────────────────────────────────
//
//  ┌──────────────────────────────┐  ┌──────────────────────────────┐
//  │  SDL3 ENGINE WINDOW          │  │  BROWSER WINDOW              │
//  │                              │  │  (Chrome/Firefox/etc.)       │
//  │  Vulkan scene render         │  │  localhost:5000              │
//  │  ImGuizmo gizmos             │  │  Blazor Server (in-process)  │
//  │  ImGui debug overlays        │◄►│  SignalR WebSocket           │
//  │                              │  │  Multiple tabs/pages         │
//  │  No embedded browser         │  │  Full DevTools for free      │
//  │  Zero overlay overhead       │  │  Hot-reloadable UI           │
//  └──────────────────────────────┘  └──────────────────────────────┘
//
// Single process — the Blazor Server runs in-process on a background
// thread while the SDL3/Vulkan engine drives the main thread.
// The editor scene viewport is stable (no user scripts) — play mode
// opens a separate SDL3 window for the full game runtime.

const string serverUrl = "http://localhost:5000";

// ── 1. Set up the shell registry and script compiler ─────────────────
var shellRegistry = new ShellRegistry();
var scriptsDir = Path.Combine(AppContext.BaseDirectory, "Scripts");

var compiler = new ScriptCompiler(shellRegistry)
    .WatchDirectory(scriptsDir)
    .AddReference(typeof(Engine.App).Assembly)          // Engine.Common
    .AddReference(typeof(Editor.Shell.ShellRegistry).Assembly); // Editor.Shell

// Add Engine.Entities reference if available
try { compiler.AddReference(typeof(Engine.EcsWorld).Assembly); } catch { }

// Perform initial compilation
var compileResult = compiler.Start();
Console.WriteLine($"[Editor] Script compilation: {compileResult.Message}");
foreach (var err in compileResult.Errors)
    Console.WriteLine($"[Editor]   ERROR {err.FileName}({err.Line},{err.Column}): {err.Message}");

// Log subsequent recompilations
compiler.CompilationCompleted += result =>
{
    Console.WriteLine($"[Editor] Hot-reload: {result.Message}");
    foreach (var err in result.Errors)
        Console.WriteLine($"[Editor]   ERROR {err.FileName}({err.Line},{err.Column}): {err.Message}");
};

// ── 2. Start the Blazor Server in-process (non-blocking) ────────────
var server = await EditorServerHost.StartAsync(serverUrl, registry: shellRegistry);
Console.WriteLine($"[Editor] Blazor Server listening on {serverUrl}");

// ── 3. Open the user's preferred browser ─────────────────────────────
try
{
    Process.Start(new ProcessStartInfo(serverUrl) { UseShellExecute = true });
    Console.WriteLine($"[Editor] Opened browser → {serverUrl}");
}
catch
{
    Console.WriteLine($"[Editor] Could not open browser automatically — navigate to {serverUrl}");
}

// ── 4. Run the native SDL3/Vulkan engine window (blocks on main thread) ──
var config = Config.GetDefault(
    title: "3D Engine Editor",
    width: 1280,
    height: 720);

try
{
    new Engine.App(config)
        .AddPlugin(new DefaultPlugins())
        .Run();
}
finally
{
    // Engine window closed — gracefully shut down everything.
    Console.WriteLine("[Editor] Shutting down...");
    compiler.Dispose();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await server.StopAsync(cts.Token);
    Console.WriteLine("[Editor] Editor shut down cleanly.");
}
