using Editor.Server;
using Editor.Shell;
using Engine;

// ── Editor Architecture ──────────────────────────────────────────────────
//
//  ┌──────────────────────────────────────────────────────────────┐
//  │  SDL3 ENGINE WINDOW                                          │
//  │                                                              │
//  │  Vulkan scene render                                         │
//  │  ImGuizmo gizmos                                             │
//  │  ImGui debug overlays                                        │
//  │                                                              │
//  │  ┌────────────────────────────────────────────────────────┐  │
//  │  │  EMBEDDED WEBVIEW (Ultralight)                         │  │
//  │  │  Blazor Server UI (in-process) ◄► SignalR WebSocket    │  │
//  │  │  Hot-reloadable editor panels & inspectors             │  │
//  │  └────────────────────────────────────────────────────────┘  │
//  └──────────────────────────────────────────────────────────────┘
//
// Single process - the Blazor Server runs in-process on a background
// thread while the SDL3/Vulkan engine drives the main thread.
// The editor UI is rendered via an Ultralight webview overlay composited
// into the Vulkan render pipeline. Play mode opens a separate SDL3
// window for the full game runtime.

const string serverUrl = "http://localhost:5000";

// ── 1. Set up the shell registry and script compiler ─────────────────
var shellRegistry = new ShellRegistry();
var scriptsDir = Path.Combine(AppContext.BaseDirectory, "Shells");

var compiler = new ShellCompiler(shellRegistry)
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

// ── 3. Run the native SDL3/Vulkan engine window (blocks on main thread) ──
//       The embedded Ultralight webview connects to the Blazor Server above.
var config = Config.GetDefault(
    title: "3D Engine Editor",
    width: 1920,
    height: 1080);

try
{
    new Engine.App(config)
        .AddPlugin(new DefaultPlugins())
        .AddPlugin(new WebViewPlugin { InitialUrl = serverUrl })
        .Run();
}
finally
{
    // Engine window closed - gracefully shut down everything.
    Console.WriteLine("[Editor] Shutting down...");
    compiler.Dispose();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await server.StopAsync(cts.Token);
    Console.WriteLine("[Editor] Editor shut down cleanly.");
}
