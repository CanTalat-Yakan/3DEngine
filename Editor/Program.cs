using System.Diagnostics;
using Editor.Server;
using Engine;

// ── Editor Architecture ──────────────────────────────────────────────────
//
//  ┌──────────────────────────────┐  ┌──────────────────────────────┐
//  │  SDL3 ENGINE WINDOW          │  │  BROWSER WINDOW              │
//  │                              │  │  (Chrome/Firefox/etc.)       │
//  │  Vulkan scene render         │  │  localhost:5000              │
//  │  ImGuizmo gizmos             │  │  Blazor Server (in-process)  │
//  │  ImGui debug overlays        │  │  SignalR WebSocket           │
//  │                              │◄►│  Multiple tabs/pages         │
//  │  No embedded browser         │  │  Full DevTools for free      │
//  │  Zero overlay overhead       │  │  Hot-reloadable UI           │
//  └──────────────────────────────┘  └──────────────────────────────┘
//
// Single process — the Blazor Server runs in-process on a background
// thread while the SDL3/Vulkan engine drives the main thread.
// The editor scene viewport is stable (no user scripts) — play mode
// opens a separate SDL3 window for the full game runtime.

const string serverUrl = "http://localhost:5000";

// ── 1. Start the Blazor Server in-process (non-blocking) ────────────
var server = await EditorServerHost.StartAsync(serverUrl);
Console.WriteLine($"[Editor] Blazor Server listening on {serverUrl}");

// ── 2. Open the user's preferred browser ─────────────────────────────
try
{
    Process.Start(new ProcessStartInfo(serverUrl) { UseShellExecute = true });
    Console.WriteLine($"[Editor] Opened browser → {serverUrl}");
}
catch
{
    Console.WriteLine($"[Editor] Could not open browser automatically — navigate to {serverUrl}");
}

// ── 3. Run the native SDL3/Vulkan engine window (blocks on main thread) ──
var config = Config.GetDefault(
    title: "3D Engine Editor — Scene Viewport",
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
    // Engine window closed — gracefully shut down the Blazor Server.
    Console.WriteLine("[Editor] Shutting down Blazor Server...");
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await server.StopAsync(cts.Token);
    Console.WriteLine("[Editor] Editor shut down cleanly.");
}
