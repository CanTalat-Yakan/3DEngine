<p align="center" style="text-align:center">
  <img src=".github/assets/icon.png" alt="3D Engine Icon" width="256"/>
</p>

<h1 align="center" style="text-align:center">3D Engine</h1>
<h4 align="center" style="text-align:center">C# 3D Game Engine (Vulkan + SDL3).</h4>
<p align="center" style="text-align:center">Editor built with Blazor Server + Ultralight WebView.</p>

<p align="center" style="text-align:center">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10.0-512BD4">
  <img alt="Graphics API" src="https://img.shields.io/badge/Graphics-Vulkan-AC162C">
  <img alt="Windowing" src="https://img.shields.io/badge/Windowing-SDL3-0B7BB2">
  <img alt="Editor" src="https://img.shields.io/badge/Editor-Blazor%20Server-6C3FA0">
</p>

<p align="center" style="text-align:center">
  <img alt="Status" src="https://img.shields.io/badge/Status-Early%20Preview-yellow">
  <img alt="Platforms" src="https://img.shields.io/badge/Platforms-Linux%20%7C%20Windows%20%7C%20macOS-lightgrey">
  <a href="https://discord.gg/c3UtTVNbRb"><img alt="Discord" src="https://img.shields.io/discord/308323056592486420?logo=discord&logoColor=white&color=5865F2"></a>
  <a href="https://deepwiki.com/CanTalat-Yakan/3DEngine"><img alt="Ask DeepWiki" src="https://deepwiki.com/badge.svg"></a>
</p>

<p align="center" style="text-align:center"><sub>Last updated: April 9, 2026</sub></p>

<p align="center" style="text-align:center">
  <img src=".github/assets/3dengine.png" alt="3D Engine Screenshot" width="720"/>
</p>

<!-- TOC -->

- [Overview](#overview)
- [Design Goals](#design-goals)
- [Current Status](#current-status)
- [Architecture](#architecture)
- [Features](#features)
    - [Runtime](#runtime)
    - [Editor](#editor)
- [Tech Stack](#tech-stack)
- [Supported Platforms](#supported-platforms)
- [Build and Run](#build-and-run)
- [Quick Start](#quick-start)
- [How It Works](#how-it-works)
    - [Stages and the Schedule](#stages-and-the-schedule)
    - [Parallel Scheduling](#parallel-scheduling)
    - [Plugins](#plugins)
    - [Resources and Events](#resources-and-events)
    - [Asset Pipeline](#asset-pipeline)
    - [ECS: Entities, Components, and Commands](#ecs-entities-components-and-commands)
    - [ECS Iteration Modes](#ecs-iteration-modes)
    - [Change Tracking](#change-tracking)
    - [Entity Generations](#entity-generations)
    - [ECS Internals and File Layout](#ecs-internals-and-file-layout)
    - [Behavior System (Attribute-Based ECS)](#behavior-system-attribute-based-ecs)
    - [Run Conditions and Toggle Keys](#run-conditions-and-toggle-keys)
    - [Native ECS Style (Manual Systems)](#native-ecs-style-manual-systems)
    - [Source Generator](#source-generator)
    - [Render Pipeline](#render-pipeline)
    - [Renderer File Layout](#renderer-file-layout)
    - [Editor Architecture](#editor-architecture)
    - [Logging](#logging)
    - [FAQ](#faq)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)

<!-- /TOC -->

## Overview

A cross-platform 3D game engine written in C# targeting .NET 10. The runtime is built on **Vulkan** for rendering and
**SDL3** for windowing and input. An editor is included, powered by an in-process **Blazor Server** rendered through an
embedded **Ultralight** WebView overlay composited into the Vulkan pipeline.

The engine features a Bevy-inspired staged execution loop with a **parallel scheduler**, a cache-friendly
**sparse-set ECS**, and an attribute-based **behavior system** driven by a Roslyn source generator. You can author
gameplay logic in two ways:

- **Behavior system** (attribute-based): mark a struct with `[Behavior]`, add methods with stage attributes like
  `[OnUpdate]`, and the generator wires them into the schedule automatically.
- **Native ECS style**: manually add systems to stages and work with `EcsWorld` queries and `EcsCommands`.

## Design Goals

- **Ergonomic ECS** - Lightweight world + staged schedule; attribute-driven behaviors with run
  conditions and keyboard toggles to minimize boilerplate.
- **Explicit Rendering** - Vulkan-first with a structured extract → prepare → queue → graph pipeline; no hidden
  abstractions.
- **Parallel by Default** - Resource-access metadata on `SystemDescriptor` enables automatic parallel batching within
  stages; conservative fallback for unannotated systems.
- **Extensibility** - Plugins compose engine services; source generation handles repetitive registration; typed event
  queues for decoupled communication.
- **Cross-Platform** - Linux, Windows, and macOS (MoltenVK) targeted with minimal platform-specific code in user land.
- **Separation of Concerns** - Runtime (game loop, ECS, renderer) cleanly separated from editor tooling (Blazor Server,
  WebView, hot-reloadable shell scripts).
- **Fast Iteration** - Hot-reload for editor UI via Roslyn-based `ShellCompiler`; inspectable runtime overlays via
  ImGui; structured logging to file and console.

## Current Status

The engine is in **early preview**. Core subsystems are functional but APIs are evolving and breaking changes are
expected.

What works today:

- SDL3 windowing with Vulkan backend and SDL software-renderer fallback.
- Staged update loop with `Startup`, per-frame stages (`First` → `Last`), and `Cleanup`.
- Parallel scheduler with resource-access batching, run conditions, and `ScheduleDiagnostics`.
- Sparse-set ECS with generational entity pool, multi-type queries, zero-allocation ref iterators, change tracking,
  and deferred command buffers.
- Behavior source generator supporting `[With]`/`[Without]`/`[Changed]` filters, `[RunIf]` conditions, and
  `[ToggleKey]` keyboard shortcuts.
- Vulkan graphics device (instance, physical device, logical device, swapchain, pipelines, buffers, images,
  descriptors, sync) via Vortice.Vulkan + VMA. `NullGraphicsDevice` for headless runs and unit tests.
- Render pipeline with extract → prepare → queue → render-graph execution model, including mesh rendering with
  `Camera`, `Mesh`, `Material`, and `Transform` components, opaque/transparent render phases, pipeline caching,
  dynamic buffer allocation, and per-object push constants.
- Asset pipeline with background threaded loading, path deduplication, typed `Assets<T>` storage, `Handle<T>` handles,
  `AssetEvent<T>` lifecycle events, pluggable `IAssetLoader<T>`/`IAssetReader` sources, and hot-reload via file
  watching.
- GLSL → SPIR-V shader compilation (both standalone and as asset via `GlslLoader`) and SPIR-V cross-reflection.
- ImGui runtime overlays (performance HUD, schedule debug HUD, WebView debug HUD).
- Ultralight WebView overlay composited into the Vulkan render pipeline.
- Editor: in-process Blazor Server with hot-reloadable Razor/C# shell scripts, served through the embedded WebView.
- Typed event queues (`Events<T>`, `EventWriter<T>`, `EventReader<T>`).
- Structured multi-provider logging (console + file).
- Unit test suite covering core, ECS, graphics, renderer, and editor subsystems.

See `Engine/Program.cs` for the runtime entry point and `Editor/Program.cs` for the editor entry point.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  3DEngine.sln                                                   │
│                                                                 │
│  Engine.Application SDL3 window, input backend, ImGui host      │
│  │                                                              │
│  Engine.Common ─── App, World, Schedule, Config, Logger,        │
│  │                 Events, Time, Input, Stage                   │
│  │                                                              │
│  Engine.Entities ─ EcsWorld, EcsCommands, BehaviorContext,      │
│  │                 Attributes, BehaviorsPlugin                  │
│  │                                                              │
│  Engine.Generator  Roslyn source generator for [Behavior]       │
│  │                                                              │
│  Engine.Files ──── AssetServer, Assets<T>, Handle<T>,           │
│  │                 AssetEvent<T>, IAssetLoader<T>, IAssetReader,│
│  │                 FileAssetReader, FileWatcher, hot-reload     │
│  │                                                              │
│  Engine.Graphics ─ Vulkan device, swapchain, pipelines,         │
│  │                 buffers, images, SPIR-V compilation,         │
│  │                 NullGraphicsDevice (headless/test)           │
│  │                                                              │
│  Engine.Renderer ─ Renderer, RenderGraph, RenderWorld,          │
│  │                 extract/prepare/queue systems, render phases,│
│  │                 Camera/Mesh/Material/Transform components,   │
│  │                 PipelineCache, DynamicBufferAllocator, ImGui │
│  │                                                              │
│  Engine.WebView ── Ultralight integration, WebView overlay      │
│  │                                                              │
│  Engine ────────── Composition root, DefaultPlugins,            │
│  │                 sample behaviors (HUD, stress test)          │
│  │                                                              │
│  Editor ────────── Editor entry point, ShellCompiler host       │
│  Editor.Server ─── Blazor Server (in-process), Razor pages      │
│  Editor.Shell ──── Shell registry, descriptors, builders        │
│  Editor.Compiler ─ Roslyn hot-reload compiler for shells        │
│  │                                                              │
│  Tests ─────────── xUnit tests (Common, Entities, Graphics,     │
│                     Renderer, Editor)                           │
└─────────────────────────────────────────────────────────────────┘
```

## Features

### Runtime

- **Staged update loop** - `Startup` (once) → `First` → `PreUpdate` → `Update` → `PostUpdate` → `Render` → `Last`
  (per frame) → `Cleanup` (once on exit).
- **Parallel scheduler** - Systems within a stage run in parallel batches based on declared `Read<T>`/`Write<T>`
  resource-access metadata. `ScheduleDiagnostics` provides per-stage and per-system timing.
- **Cache-friendly ECS** - Sparse-set storage, generational entity pool, per-frame bitset change tracking,
  zero-allocation ref iterators, multi-type queries (up to 3), `TransformEach`, `ParallelTransformEach`, and raw span
  access.
- **Behavior source generator** - `[Behavior]` structs with stage attributes (`[OnStartup]` through `[OnCleanup]`),
  component filters (`[With]`, `[Without]`, `[Changed]`), run conditions (`[RunIf]`), and keyboard toggles
  (`[ToggleKey]`).
- **Deferred commands** - `EcsCommands` queue spawn/despawn/add/remove operations, flushed automatically at
  `PostUpdate`.
- **Typed event system** - `Events<T>` queues with `EventWriter<T>` and `EventReader<T>` for decoupled, type-safe
  inter-system communication.
- **Plugin model** - `DefaultPlugins` aggregates: `AssetPlugin`, `AppWindowPlugin`, `AppExitPlugin`,
  `ExceptionsPlugin`, `TimePlugin`, `InputPlugin`, `EcsPlugin`, `BehaviorsPlugin`, `SdlImGuiPlugin`,
  `SdlRendererPlugin`, `VulkanWebViewPlugin`, `VulkanImGuiPlugin`. Also registers `GlslLoader` with the `AssetServer`.
- **Vulkan graphics device** - Instance creation, physical device selection, logical device, swapchain management,
  pipeline creation, buffer/image allocation (VMA), descriptor sets, and synchronization primitives.
  `NullGraphicsDevice` provides a no-op implementation for headless runs and unit tests.
- **Render pipeline** - Extract → BeginFrame → Prepare → Graph execution (Update → auto-barrier → Run per node) →
  EndFrame. Built-in extract systems (`CameraExtract`, `MeshMaterialExtract`, `ClearColorExtract`), prepare systems
  (`MeshPrepare`, `QueueMeshPhaseItems`), and the `MainPassNode` with mesh shader pipeline, per-object push constants
  (`MeshPushConstants`), camera UBO, and `ActiveSwapchainPass` pattern (pass stays open for overlay nodes). Render
  components: `Camera`, `Mesh`, `Material`, `Transform`, `RenderMeshInstance`, `ExtractedView`. Render phases:
  `Opaque3dPhase` (front-to-back), `Transparent3dPhase` (back-to-front) with `IPhaseItem` and `IDrawFunction<T>`.
  GPU resource management: `PipelineCache` for deduplicating compiled pipelines, `DynamicBufferAllocator` for
  frame-scoped transient GPU buffers, `MeshGpuRegistry` for caching uploaded vertex buffers.
- **Asset pipeline** - `AssetServer` with background threaded loading, path deduplication, typed `Assets<T>` storage,
  `Handle<T>` handles, `AssetEvent<T>` lifecycle events (`Added`, `Modified`, `LoadedWithDependencies`), pluggable
  `IAssetLoader<T>` / `IAssetReader` sources (`FileAssetReader`, `EmbeddedAssetReader`), dependency tracking, and
  hot-reload via `FileAssetWatcher`. Built-in loaders: `ByteArrayLoader`, `StringLoader`, `GlslLoader` (GLSL → SPIR-V
  as asset).
- **Shader pipeline** - GLSL → SPIR-V compilation via `Vortice.ShaderCompiler`; SPIR-V cross-reflection via
  `Vortice.SpirvCross`.
- **ImGui overlays** - Performance HUD (FPS, frame time, entity count), schedule debug HUD (batch composition,
  conflict notes), and WebView debug HUD. Togglable via `[ToggleKey]`.
- **WebView overlay** - Ultralight-based HTML/CSS/JS rendering composited into the Vulkan pipeline with full SDL3
  input forwarding.
- **Structured logging** - Multi-provider logging (console with ANSI colors, file output) with category scoping,
  severity levels (`Trace` through `Critical`), and frame-level trace suppression.

### Editor

- **Blazor Server** - In-process ASP.NET Core host serving a Razor component tree over SignalR WebSocket.
- **Embedded WebView** - Ultralight renders the Blazor UI directly inside the SDL3/Vulkan engine window.
- **Hot-reloadable shells** - `ShellCompiler` watches `.cs`/`.razor`/`.css` files, recompiles via Roslyn on change,
  and hot-swaps editor panels without restarting.
- **Shell registry** - `ShellRegistry` + `ShellDescriptor` system for dynamically discovered editor panels and
  inspectors.
- **Single process** - Engine and editor run in one process; Blazor Server on a background thread, SDL3/Vulkan on the
  main thread.

## Tech Stack

| Component | Library / Package | Purpose |
|---|---|---|
| Windowing & Input | [SDL3-CS](https://github.com/ppy/SDL3-CS) (preview) | Cross-platform window, input, audio |
| GPU API | [Vortice.Vulkan](https://github.com/amerkoleci/Vortice.Vulkan) 3.2.1 | Vulkan bindings |
| Memory Allocator | Vortice.VulkanMemoryAllocator 1.7.0 | GPU memory management (VMA) |
| Shader Compilation | Vortice.ShaderCompiler 1.9.0 | GLSL → SPIR-V |
| Shader Reflection | Vortice.SPIRV 1.0.5 + Vortice.SpirvCross 1.5.4 | SPIR-V cross-reflection |
| Debug UI | Twizzle.ImGui-Bundle.NET 1.91.5.2 | ImGui runtime overlays |
| Asset Import | AssimpNet 5.0.0-beta1 | 3D model import (FBX, glTF, OBJ, …) |
| Scene Interchange | UniversalSceneDescription 6.0.0 | USD scene format |
| WebView | UltralightNet (vendored) | HTML/CSS/JS overlay in Vulkan |
| Editor UI | BlazorBlueprint Components | Blazor component library |
| Hot Reload | Editor.Compiler (Roslyn-based) | Runtime recompilation of shell scripts |
| Serialization | Newtonsoft.Json 13.0.5 | Configuration and data serialization |
| Source Generator | Engine.Generator (Roslyn) | `[Behavior]` system code generation |

## Supported Platforms

| Platform | Status | Notes |
|---|---|---|
| **Linux** | Supported | X11 and Wayland via SDL3. Requires recent Vulkan drivers (Mesa / NVIDIA / AMD). |
| **Windows** | Supported | Vulkan-capable GPU + drivers. |
| **macOS** | Experimental | Via MoltenVK (Vulkan portability). |

## Build and Run

**Prerequisites:**

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (preview SDK may be required).
- A working Vulkan driver/runtime for your GPU.

**Clone and build:**

```bash
git clone https://github.com/CanTalat-Yakan/3DEngine.git
cd 3DEngine

# Restore and build all projects
dotnet build

# Run the engine runtime
dotnet run --project Engine

# Run the editor
dotnet run --project Editor
```

Open in an IDE (Rider / Visual Studio / VS Code) using `3DEngine.sln`.

**Run tests:**

```bash
dotnet test
```

**Notes:**

- Native binaries for SDL3, ImGui, and Ultralight are bundled via NuGet. On Linux you still need standard system
  libraries (X11/Wayland, audio, etc.) and up-to-date GPU drivers.
- If Vulkan initialization fails, verify your driver installation and ensure validation layers are either installed or
  disabled.
- The editor requires the Blazor Server port (`http://localhost:5000` by default) to be available.

## Quick Start

### Behavior-based (automatic)

```csharp
[Behavior]
public struct Spinner
{
    public float Angle;

    [OnStartup]
    public static void Spawn(BehaviorContext ctx)
    {
        var e = ctx.Ecs.Spawn();
        ctx.Ecs.Add(e, new Spinner { Angle = 0f });
    }

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Angle += (float)ctx.Time.DeltaSeconds * 90f;
        Console.WriteLine($"Entity {ctx.EntityId} angle: {Angle:0.00}°");
    }
}
```

1. Add the struct to any project that references `Engine.Entities`.
2. Build - the source generator emits systems automatically.
3. Run - the behavior executes per entity each frame. No manual registration needed.

### Plugin-based (manual)

```csharp
var config = Config.GetDefault(
    title: "My Game",
    width: 1280,
    height: 720,
    graphics: GraphicsBackend.Vulkan);

new App(config)
    .AddPlugin(new DefaultPlugins())
    .AddPlugin(new GamePlugin())
    .Run();

public sealed class GamePlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddSystem(Stage.Startup, world =>
        {
            var ecs = world.Resource<EcsWorld>();
            var e = ecs.Spawn();
            ecs.Add(e, new Spinner { Angle = 0f });
        });

        app.AddSystem(Stage.Update, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            var time = world.Resource<Time>();
            foreach (var (e, spinner) in ecs.Query<Spinner>())
            {
                var s = spinner;
                s.Angle += (float)time.DeltaSeconds * 45f;
                ecs.Update(e, s);
            }
        }, "GamePlugin.SpinnerUpdate")
        .Read<Time>()
        .Write<EcsWorld>());
    }
}
```

## How It Works

### Stages and the Schedule

The engine drives a staged loop (see `Engine.Common/Stage.cs`):

- **`Startup`** - runs once before the main loop.
- **Per-frame stages** - `First` → `PreUpdate` → `Update` → `PostUpdate` → `Render` → `Last`.
- **`Cleanup`** - runs once after the main loop exits (resource teardown).

Systems are `SystemFn(World world)` delegates registered to stages via `App.AddSystem(stage, system)` and executed by
the `Schedule`.

```
 Startup (once)
    │
    ▼
 ┌─ First ──► PreUpdate ──► Update ──► PostUpdate ──► Render ──► Last ─┐
 │                                                                     │
 └──────────────────────── next frame ─────────────────────────────────┘
    │
    ▼
 Cleanup (once)
```

### Parallel Scheduling

Within each stage, systems are partitioned into **execution batches** based on declared resource access:

```csharp
app.AddSystem(Stage.Update, new SystemDescriptor(MySystem, "Physics.Integrate")
    .Read<Time>()
    .Write<EcsWorld>()
    .RunIf(world => world.Resource<GameState>().IsPlaying));
```

- **`Read<T>()`** - declares a read-only dependency. Multiple readers can run in parallel.
- **`Write<T>()`** - declares a read-write dependency. Writers conflict with all other accessors of the same type.
- **`RunIf(predicate)`** - conditional execution; the system is skipped when the predicate returns `false`.
- **`MainThreadOnly()`** - forces the system onto the main thread (required for SDL/GPU calls).

Systems without explicit metadata are conservatively serialized. `Startup`, `Render`, and `Cleanup` stages are
single-threaded by default; all other stages are parallel.

`ScheduleDiagnostics` (inserted as a world resource) exposes per-stage timing, per-system timing, batch composition,
and conflict notes for debugging.

### Plugins

Plugins implement `IPlugin` and configure the app during a one-time `Build` phase:

```csharp
public sealed class PhysicsPlugin : IPlugin
{
    public void Build(App app)
    {
        // Insert resources
        app.World.InitResource<PhysicsWorld>();

        // Register systems with resource-access metadata
        app.AddSystem(Stage.PreUpdate, new SystemDescriptor(world =>
        {
            var physics = world.Resource<PhysicsWorld>();
            var time = world.Resource<Time>();
            physics.Step(time.DeltaSeconds);
        }, "Physics.Step")
        .Read<Time>()
        .Write<PhysicsWorld>());
    }
}
```

`DefaultPlugins` aggregates the standard engine plugin set:

| Plugin | Responsibility |
|---|---|
| `AssetPlugin` | `AssetServer` resource, `FileAssetReader` source, background loading, per-frame drain and event clearing |
| `AppWindowPlugin` | SDL3 window, `IMainLoopDriver`, `IInputBackend`, Vulkan surface source |
| `AppExitPlugin` | Graceful shutdown on quit events |
| `ExceptionsPlugin` | Unhandled exception logging |
| `TimePlugin` | Frame timing (`Time` resource) |
| `InputPlugin` | Keyboard, mouse, text input (`Input` resource) |
| `EcsPlugin` | `EcsWorld` + `EcsCommands` resources, PostUpdate command flush |
| `BehaviorsPlugin` | Discovers and invokes source-generated behavior registrations |
| `SdlImGuiPlugin` | SDL3-backed ImGui host |
| `SdlRendererPlugin` | Vulkan renderer initialization, extract/prepare system wiring, per-frame rendering |
| `VulkanWebViewPlugin` | WebView render node in the Vulkan pipeline |
| `VulkanImGuiPlugin` | ImGui render node in the Vulkan pipeline |

`DefaultPlugins.Build` also registers the `GlslLoader` with the `AssetServer` so that GLSL shader files can be loaded
as SPIR-V bytecode assets.

### Resources and Events

**`World`** is a thread-safe, type-keyed resource container:

```csharp
// Insert
app.InsertResource(new MyService());
world.InsertResource(value);

// Retrieve (throws if missing)
var svc = world.Resource<MyService>();

// Safe retrieval
if (world.TryGetResource<MyService>(out var s)) { ... }

// Get or create with default
var res = world.InitResource<MyService>();
```

Common resources: `Config`, `EcsWorld`, `EcsCommands`, `AppWindow`, `Time`, `Input`, `AssetServer`, `Renderer`,
`ScheduleDiagnostics`, `WebViewInstance`.

**`Time`** - frame timing updated each frame by `TimePlugin`:

```csharp
var time = world.Resource<Time>();
double dt   = time.DeltaSeconds;      // clamped delta (use for gameplay)
double raw  = time.RawDeltaSeconds;   // un-clamped wall-clock delta
double fps  = time.SmoothedFps;       // exponentially smoothed FPS
ulong frame = time.FrameCount;        // total frames since start
```

**`Input`** - keyboard, mouse, and text input state updated by `InputPlugin`:

```csharp
var input = world.Resource<Input>();

// Keyboard
if (input.KeyPressed(Key.Space))  { /* first frame Space is down */ }
if (input.KeyDown(Key.W))         { /* held down continuously  */ }
if (input.KeyReleased(Key.Escape)){ /* released this frame     */ }

// Mouse
if (input.MousePressed(MouseButton.Left)) { /* click */ }
var (mx, my) = input.MousePosition;
var (dx, dy) = input.MouseDelta;
float scroll = input.WheelY;

// Text input (typed characters this frame)
foreach (char c in input.TextInput)
    Console.Write(c);
```

**`Events<T>`** provides typed, thread-safe event queues for decoupled inter-system communication:

```csharp
// Direct queue access
Events.Get<DamageEvent>(world).Send(new DamageEvent(target, 25));
foreach (var evt in Events.Get<DamageEvent>(world).Read())
    ApplyDamage(evt);

// Typed writer/reader handles (zero-allocation ref structs)
var writer = EventWriter<DamageEvent>.Get(world);
writer.Send(new DamageEvent(target, 25));
writer.SendBatch(stackalloc[] { evt1, evt2 });

var reader = EventReader<DamageEvent>.Get(world);
foreach (var evt in reader.Read())
    ApplyDamage(evt);
var drained = reader.Drain(); // read + clear atomically

// World extension shortcuts
world.SendEvent(new DamageEvent(target, 25));
foreach (var evt in world.ReadEvents<DamageEvent>())
    ApplyDamage(evt);
world.ClearEvents<DamageEvent>();
```

Events accumulate during a frame and should be cleared once per frame (typically in `Stage.Last`).

### Asset Pipeline

The `AssetServer` is the central asset management coordinator registered as a world resource by `AssetPlugin`. It
handles async background loading, path deduplication, typed storage, dependency tracking, and hot-reload via file
watching.

**Architecture:**

```
 AssetPlugin (Stage.PreUpdate) ─── drain completed loads → Assets<T> + fire AssetEvent<T>
 AssetPlugin (Stage.Last) ──────── clear asset events
 AssetPlugin (Stage.Cleanup) ───── dispose AssetServer

 ┌──────────────────────────────────────────────────────────┐
 │  AssetServer                                             │
 │                                                          │
 │  Sources: FileAssetReader, EmbeddedAssetReader, ...      │
 │  Loaders: GlslLoader, ByteArrayLoader, StringLoader, ...│
 │  Workers: N background threads (Channel<LoadRequest>)    │
 │  Tracking: path→Handle deduplication, LoadState per ID   │
 │  Hot-reload: FileAssetWatcher → re-enqueue on change     │
 └──────────────────────────────────────────────────────────┘
```

**Loading assets:**

```csharp
// In a plugin's Build method - register a custom loader
var server = world.Resource<AssetServer>();
server.RegisterLoader(new TextureLoader());

// Async loading (returns a handle immediately, loads on background thread)
Handle<Texture> tex = server.Load<Texture>("textures/ground.png");

// Synchronous loading (blocks - use sparingly, e.g. during Startup)
byte[] spv = server.LoadSync<byte[]>("shaders/mesh.vert.glsl");

// Check load state
LoadState state = server.GetLoadState(tex);

// Next frame, read from typed storage
var assets = world.Resource<Assets<Texture>>();
if (assets.TryGet(tex, out var texture))
    BindTexture(texture);
```

**Asset events** fire when assets complete loading or are modified by hot-reload:

```csharp
foreach (var evt in world.ReadEvents<AssetEvent<Texture>>())
{
    if (evt.Kind == AssetEventKind.Added)
        Console.WriteLine($"Texture loaded: {evt.Handle.Path}");
    if (evt.Kind == AssetEventKind.Modified)
        Console.WriteLine($"Texture hot-reloaded: {evt.Handle.Path}");
}
```

**Custom loaders** implement `IAssetLoader<T>`:

```csharp
public sealed class TextureLoader : IAssetLoader<Texture>
{
    public string[] Extensions => [".png", ".jpg"];

    public async Task<AssetLoadResult<Texture>> LoadAsync(AssetLoadContext context, CancellationToken ct)
    {
        var bytes = await context.ReadAllBytesAsync(ct);
        var texture = DecodeTexture(bytes);
        return AssetLoadResult<Texture>.Ok(texture);
    }
}
```

**Custom sources** implement `IAssetReader`. Built-in sources include `FileAssetReader` (filesystem) and
`EmbeddedAssetReader` (embedded resources). Sources are probed in registration order.

**Hot-reload** is enabled per `AssetPlugin` configuration:

```csharp
app.AddPlugin(new AssetPlugin
{
    AssetDirectory = "/path/to/assets",
    WatchForChanges = true,
    WorkerThreads = 4,
});
```

When enabled, `FileAssetWatcher` monitors the filesystem source directory and re-enqueues changed assets
for background reloading. Modified assets trigger `AssetEvent<T>.Modified` events.

### ECS: Entities, Components, and Commands

**Entities** are `int` IDs backed by a generational pool. Create with `ecs.Spawn()`, remove with `ecs.Despawn(id)`.

**Component API:**

| Method | Description |
|---|---|
| `Add<T>(id, comp)` | Attach a component (overwrites if present). |
| `Update<T>(id, comp)` | Set + mark as changed this frame. |
| `Mutate<T>(id, fn)` | Transform in-place via a function, marks changed. |
| `GetRef<T>(id)` | Returns a `ref` to the live component (throws if missing). |
| `TryGet<T>(id, out comp)` | Safe read. |
| `Has<T>(id)` | Existence check. |
| `Remove<T>(id)` | Detach a component. |
| `Changed<T>(id)` | Was this component modified this frame? |
| `Count<T>()` | Number of entities with this component. |
| `EntitiesWith<T>()` | Span of entity IDs that have this component. |
| `Reserve<T>(n, hint)` | Pre-allocate storage to reduce resizing. |

```csharp
var ecs = world.Resource<EcsWorld>();

// Spawn and attach components
var e = ecs.Spawn();
ecs.Add(e, new Position { X = 0, Y = 0 });
ecs.Add(e, new Velocity { X = 1, Y = 0 });

// Read
if (ecs.TryGet<Position>(e, out var pos))
    Console.WriteLine($"Position: {pos.X}, {pos.Y}");

// Update (marks changed)
ecs.Update(e, new Position { X = 5, Y = 10 });

// Mutate in-place via function (marks changed)
ecs.Mutate<Position>(e, p => p with { X = p.X + 1 });

// Direct ref access for hot paths
ref var vel = ref ecs.GetRef<Velocity>(e);
vel.X += 0.1f;

// Bulk operations
ecs.Reserve<Position>(10_000);                   // pre-allocate storage
ReadOnlySpan<int> ids = ecs.EntitiesWith<Position>(); // all entities with Position
bool dirty = ecs.Changed<Position>(e);           // was it modified this frame?

// Despawn (disposes IDisposable components)
ecs.Despawn(e);
```

**Queries:**

```csharp
ecs.Query<T>()              // single component
ecs.Query<T1, T2>()         // two components (joins on smallest set)
ecs.Query<T1, T2, T3>()     // three components
ecs.QueryWhere<T>(predicate) // filtered by predicate
```

**Deferred commands** via `EcsCommands` - mutations queued during iteration, flushed at `PostUpdate`:

```csharp
// Spawn with components (fluent chaining)
ctx.Cmd
    .Spawn((id, ecs) =>
    {
        ecs.Add(id, new Position { X = 0, Y = 0 });
        ecs.Add(id, new Velocity { X = 1, Y = 0 });
    })
    .Spawn((id, ecs) => ecs.Add(id, new Position()));

// Deferred add / remove / despawn
ctx.Cmd.Add(entityId, new Health { Value = 100 });
ctx.Cmd.Remove<Poison>(entityId);
ctx.Cmd.Despawn(entityId);
```

Commands are flushed automatically at `PostUpdate` by `EcsPlugin`. On `Despawn`, components implementing `IDisposable`
are disposed.

### ECS Iteration Modes

| Mode | Allocation | Marks Changed | Best For |
|---|---|---|---|
| `Query<T>()` | Value-copy tuples | No | Read-only scans, LINQ |
| `QueryRef<T>()` | Zero-allocation refs | Yes | In-place mutation |
| `GetSpan<T>()` | Raw spans | No (manual) | Tight indexed loops |
| `TransformEach<T>(fn)` | Per-component callback | Yes | Bulk mutation |
| `ParallelTransformEach<T>(fn)` | Per-component callback | Yes | Large sets, no cross-entity deps |

```csharp
// Value-copy query
foreach (var (e, pos) in ecs.Query<Position>()) { /* read pos */ }

// Multi-component query (joins on smallest set)
foreach (var (e, pos, vel) in ecs.Query<Position, Velocity>())
    ecs.Update(e, new Position { X = pos.X + vel.X, Y = pos.Y + vel.Y });

// Filtered query with predicate
foreach (var (e, hp) in ecs.QueryWhere<Health>(h => h.Value <= 0))
    ecs.Despawn(e);

// Zero-allocation ref iteration (marks changed)
foreach (var rc in ecs.QueryRef<Velocity>())
    rc.Component.dx += 1;

// Two-component ref iteration
foreach (var rc in ecs.QueryRef<Position, Velocity>())
    rc.C1.x += rc.C2.dx;

// Mutate in-place via function (marks changed, no-op if missing)
ecs.Mutate<Health>(e, h => h with { Value = h.Value - 10 });

// Transform helper (marks changed)
ecs.TransformEach<Position>((e, p) => { p.x += 1; return p; });

// Parallel transform
ecs.ParallelTransformEach<Position>((e, p) => { p.x += 1; return p; });

// Raw span access (manual change marking)
var span = ecs.GetSpan<Mass>();
for (int i = 0; i < span.Entities.Length; i++)
    span.Components[i].value *= 2;
ecs.TransformEach<Mass>((e, m) => m); // mark all changed
```

### Change Tracking

- Each `ComponentStore<T>` maintains a per-entity "changed this frame" bitset aligned to dense storage.
- The bit is set by `Update`, `TransformEach`, `ParallelTransformEach`, and `QueryRef`.
- `Changed<T>(id)` reads the bit; bits are cleared at `BeginFrame()` (executed in `Stage.First` by `DefaultPlugins`).

### Entity Generations

- The `EntityPool` tracks a generation counter per entity ID to guard against stale references.
- On `Despawn(id)`, the generation is incremented and the slot enters a free list for reuse.
- Public API remains ID-only. For diagnostics, inspect via `ecs.GetGeneration(id)`.

### ECS Internals and File Layout

**Storage model:** Sparse-set layout - sparse index array + dense arrays for entities and components. O(1) add, remove,
and lookup; cache-friendly sequential iteration.

**Partial class split:**

| File | Contents |
|---|---|
| `EcsWorld.cs` | Type declarations, internal storage, `IComponentStore` interface |
| `EcsWorld.API.cs` | Public entity/component/query API (`Spawn`, `Add`, `Query`, etc.) |
| `EcsWorld.Components.cs` | `ComponentStore<T>` implementation, CRUD, spans |
| `EcsWorld.Pool.cs` | `EntityPool` - generational ID allocator with free list |
| `EcsWorld.SparseSet.cs` | `SparseSet<T>` data structure |
| `EcsWorld.RefIterators.cs` | `RefEnumerable<T>`, `RefEnumerable<T1,T2>`, transforms |

Supporting files in `Engine.Entities`:

| File | Contents |
|---|---|
| `BehaviorAttributes.cs` | `[Behavior]`, `[OnUpdate]`, `[With]`, `[Without]`, `[Changed]`, `[RunIf]`, `[ToggleKey]` |
| `BehaviorConditions.cs` | Built-in run condition factories (`ResourceIs<T>`, etc.) |
| `BehaviorContext.cs` | `BehaviorContext` - per-invocation context (World, Ecs, Cmd, Time, Input, EntityId) |
| `BehaviorsPlugin.cs` | Runtime discovery and registration of source-generated behaviors |
| `EcsCommands.cs` | Deferred command buffer (Spawn, Despawn, Add, Remove) |
| `EcsPlugin.cs` | Plugin inserting `EcsWorld` + `EcsCommands`, PostUpdate flush system |

### Behavior System (Attribute-Based ECS)

Author gameplay in a script-like way:

1. Mark a struct with **`[Behavior]`**.
2. Add methods with **stage attributes**: `[OnStartup]`, `[OnFirst]`, `[OnPreUpdate]`, `[OnUpdate]`,
   `[OnPostUpdate]`, `[OnRender]`, `[OnLast]`, `[OnCleanup]`.
3. Optionally add **filters** on instance methods:
   - `[With(typeof(Position), typeof(Velocity))]` - require components.
   - `[Without(typeof(Disabled))]` - exclude components.
   - `[Changed(typeof(Transform))]` - run only if changed this frame.

**Static vs instance methods:**

- **Static** methods run once per stage invocation and receive `BehaviorContext`.
- **Instance** methods run per entity that has this behavior component. The generator iterates
  `ecs.Query<YourBehavior>()`, sets `ctx.EntityId`, calls your method, and writes back via `ecs.Update`.

> The generator supports `With` joins of up to 3 component types. Beyond that, it falls back to querying the behavior
> component and applies `Without`/`Changed` checks inside the loop.

**`BehaviorContext`** provides convenient access:

| Property | Type | Description |
|---|---|---|
| `World` | `World` | Global resource container |
| `Ecs` | `EcsWorld` | Entity/component storage and queries |
| `Cmd` | `EcsCommands` | Deferred mutations (applied at PostUpdate) |
| `Time` | `Time` | Frame timing (delta, elapsed, FPS) |
| `Input` | `Input` | Keyboard, mouse, text input state |
| `EntityId` | `int` | Current entity (instance methods only) |
| `Res<T>()` | `T` | Shortcut for `World.Resource<T>()` |

**Examples:**

```csharp
using ImGuiNET;

[Behavior]
public struct PerformanceHud
{
    [OnRender]
    public static void Draw(BehaviorContext ctx)
    {
        ImGui.Begin("Performance");
        ImGui.Text($"FPS: {ctx.Time.SmoothedFps:0}");
        ImGui.Text($"Frame: {ctx.Time.DeltaSeconds * 1000.0:0.00} ms");
        ImGui.End();
    }
}

[Behavior]
public struct Spawner
{
    public float Timer;

    [OnStartup]
    public static void Init(BehaviorContext ctx)
    {
        var e = ctx.Ecs.Spawn();
        ctx.Ecs.Add(e, new Spawner { Timer = 0f });
    }

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Timer += (float)ctx.Time.DeltaSeconds;
        Console.WriteLine($"Spawner entity {ctx.EntityId} timer: {Timer:0.00}s");
    }
}

[Behavior]
public struct HeavyBehavior : IDisposable
{
    private SomeResource _handle;

    [OnStartup]
    public static void Init(BehaviorContext ctx)
    {
        var e = ctx.Ecs.Spawn();
        ctx.Ecs.Add(e, new HeavyBehavior { _handle = new SomeResource() });
    }

    [OnUpdate]
    public void Tick(BehaviorContext ctx) => Console.WriteLine(_handle.Log());

    [OnCleanup]
    public static void Teardown(BehaviorContext ctx) { /* global cleanup */ }

    public void Dispose() => _handle?.Dispose(); // called on Despawn
}
```

### Run Conditions and Toggle Keys

**`[RunIf]`** - attach a static condition to skip a system when it returns `false`:

```csharp
[Behavior]
public struct GameplaySystem
{
    // Reference a static method, property, or field by name
    [OnUpdate]
    [RunIf(nameof(IsPlaying))]
    public static void Tick(BehaviorContext ctx) { /* ... */ }

    public static bool IsPlaying(World world)
        => world.TryGetResource<GameState>(out var s) && s.IsPlaying;
}
```

**`[ToggleKey]`** - bind a keyboard shortcut to toggle a system on/off at runtime:

```csharp
[OnRender]
[ToggleKey(Key.F3)]                          // F3 to toggle
[ToggleKey(Key.F3, KeyModifier.Alt)]         // Alt+F3 to toggle
[ToggleKey(Key.F3, DefaultEnabled = false)]  // starts disabled
public static void Draw(BehaviorContext ctx) { /* ... */ }
```

Toggle state is managed via `SystemToggleRegistry` in the world.

### Native ECS Style (Manual Systems)

Register systems directly with full control over scheduling:

```csharp
app.AddSystem(Stage.Update, new SystemDescriptor(world =>
{
    var ecs = world.Resource<EcsWorld>();
    foreach (var (e, comp) in ecs.Query<MyComponent>())
    {
        var c = comp;
        c.Value += 1;
        ecs.Update(e, c);
    }
}, "MyGame.UpdateComponents")
.Read<Time>()
.Write<EcsWorld>());
```

Behavior-generated systems and hand-written systems coexist. The behavior generator emits `SystemDescriptor`-based
registrations under the hood.

### Source Generator

`Engine.Generator` (`BehaviorGenerator`) is a Roslyn incremental source generator that scans for `[Behavior]` structs
and emits:

- **Per-behavior** `{Name}_Generated.g.cs` - static class with system lambdas for each stage method, including
  query loops, filter checks, and component write-back for instance methods.
- **`BehaviorsRegistration.g.cs`** - a single registration method marked with `[GeneratedBehaviorRegistration]`,
  discovered at runtime by `BehaviorsPlugin` via reflection.

`DefaultPlugins` includes `BehaviorsPlugin`, so behaviors are picked up automatically at build time - no manual
registration required.

### Render Pipeline

The `Renderer` orchestrates a rendering pipeline each frame:

```
 Extract ──► BeginFrame ──► Prepare ──► Graph (Update → auto-barrier → Run per node) ──► EndFrame
```

| Phase | Interface / System | Purpose |
|---|---|---|
| **Extract** | `IExtractSystem` | Copy game-world data into the `RenderWorld` ECS (render entities) |
| **BeginFrame** | `RendererContext` | Acquire swapchain image, upload camera UBO, populate `SwapchainTarget` |
| **Prepare** | `IPrepareSystem` | Upload GPU resources (vertex buffers, textures) from extracted data |
| **Graph** | `INode` / `ViewNode` | Execute render passes in topological order with typed slot edges |
| **EndFrame** | `RendererContext` | Submit command buffer and present the swapchain image |

The `RenderStage` enum defines the four conceptual phases: `Extract`, `Prepare`, `Queue`, `Execute`.

**Built-in extract systems:**

| System | Description |
|---|---|
| `ClearColorExtract` | Copies `ClearColor` resource from game world to render world |
| `CameraExtract` | Extracts `Camera` + `Transform` components into `ExtractedView` render entities (right-handed, camera looks along −Z; projection Y is flipped for Vulkan NDC) |
| `MeshMaterialExtract` | Extracts `Mesh` + `Material` + `Transform` into `RenderMeshInstance` render entities |

**Built-in prepare systems:**

| System | Description |
|---|---|
| `MeshPrepare` | Ensures GPU vertex buffers exist for all `RenderMeshInstance` entities via `MeshGpuRegistry` |
| `QueueMeshPhaseItems` | Populates `Opaque3dPhase` and `Transparent3dPhase` from extracted render data |

**Render components** (game-world ECS):

| Component | Description |
|---|---|
| `Camera` | Perspective projection parameters (FovY, Near, Far) with optional render texture target |
| `Mesh` | Raw vertex position data (`Vector3[]`) for GPU upload |
| `Material` | Simple RGBA albedo color |
| `Transform` | World-space position, rotation (quaternion), and scale |

**Render-world components** (render entities, re-created each frame):

| Component | Description |
|---|---|
| `ExtractedView` | Computed view/projection matrices and viewport dimensions (from `Camera` + `Transform`) |
| `RenderMeshInstance` | Model matrix, albedo, mesh data, vertex count, source entity ID |

**Render phases:**

- `Opaque3dPhase` - front-to-back sorted opaque draw calls.
- `Transparent3dPhase` - back-to-front sorted transparent draw calls.
- Each phase uses `RenderPhase<T>` with `IPhaseItem` items and `IDrawFunction<T>` draw functions.

**MainPassNode** is the default render graph node. It begins the swapchain render pass with `LoadOp.Clear`, sets up
the camera UBO via `DynamicBufferAllocator`, creates the `MeshPipeline` (vertex input + push constants), and drains
opaque and transparent phases via their draw functions. The render pass is left **open** and published as
`ActiveSwapchainPass` in the `RenderWorld` so that downstream overlay nodes (WebView, ImGui) can draw into the same
pass without the overhead of separate begin/end cycles. The `Renderer` ends the pass after all graph nodes have
executed.

**GPU resource management:**

- `PipelineCache` - deduplicates compiled `IPipeline` instances keyed by `GraphicsPipelineDesc`, shared across all
  render graph nodes via the `RenderWorld`.
- `DynamicBufferAllocator` - frame-aware bump allocator for transient GPU buffers (uniform, vertex, index). Maintains
  per-frame-slot arenas with automatic power-of-two growth.
- `MeshGpuRegistry` - caches uploaded vertex buffers keyed by game-world entity ID, avoiding redundant GPU uploads
  across frames.
- `RenderSurfaceInfo` - tracks the presentation surface dimensions with a monotonic revision counter for resize
  detection.
- `RenderTextures` - registry of named render texture descriptors for off-screen camera targets.

Each `INode` owns its own render passes and declares typed input/output slots (`SlotInfo`).
`ViewNode` is a convenience base that auto-iterates `ExtractedView` entities, calling a per-camera `Run` overload.
Nodes receive a `RenderContext` (wrapping `IGraphicsDevice`, `ICommandBuffer`, and `DynamicBufferAllocator`)
and a `RenderGraphContext` for accessing slot values and running sub-graphs.
`TrackedRenderPass` wraps begin/end render pass lifecycle with typed helpers for pipeline binding, draw calls, and push constants.

```csharp
// Register custom render systems and graph nodes
var renderer = world.Resource<Renderer>();
renderer.AddExtractSystem(new MyExtractSystem());
renderer.AddPrepareSystem(new MyPrepareSystem());
renderer.Graph.AddNode("mypass", new MyNode());
renderer.Graph.AddNodeEdge("main_pass", "mypass");

// Implement a custom render graph node
public class MyNode : INode
{
    public void Run(RenderGraphContext graphContext, RenderContext renderContext, RenderWorld rw)
    {
        // Reuse the open swapchain pass from MainPassNode
        var activePass = rw.TryGet<ActiveSwapchainPass>();
        if (activePass is null) return;

        var pass = activePass.Pass;
        pass.SetPipeline(myPipeline);
        pass.Draw(vertexCount: 3);
    }
}

// Spawn a renderable entity in game world
var ecs = world.Resource<EcsWorld>();
var e = ecs.Spawn();
ecs.Add(e, new Camera(fovYDegrees: 60f, near: 0.1f, far: 1000f));
ecs.Add(e, new Transform(new Vector3(0, 0, 5)));

var mesh = ecs.Spawn();
ecs.Add(mesh, new Mesh(new[] { new Vector3(0,1,0), new Vector3(-1,-1,0), new Vector3(1,-1,0) }));
ecs.Add(mesh, new Material(new Vector4(1, 0, 0, 1))); // red triangle
ecs.Add(mesh, new Transform(Vector3.Zero));
```

`RenderGraph` manages the directed acyclic graph of `INode` instances with typed slot edges and sub-graph support.
`RendererContext` wraps the `GraphicsDevice` and provides frame-scoped resources (camera UBOs, dynamic allocator,
`SwapchainTarget`). `RendererDiagnostics` tracks adapter info, surface extent, and frame statistics.

### Renderer File Layout

The `Engine.Renderer` project is organized into subdirectories by concern:

| Directory | Contents |
|---|---|
| `Abstractions/` | `INode`, `IExtractSystem`, `IPrepareSystem`, `IPhaseItem`, `IDrawFunction<T>` |
| `Components/` | `Camera`, `Mesh`, `Material`, `Transform`, `RenderMeshInstance`, `ExtractedView`, `CameraUniform` |
| `Extracts/` | `CameraExtract`, `MeshMaterialExtract` |
| `Graph/` | `RenderGraph`, `RenderContext`, `RenderGraphContext`, `SlotInfo`, `SlotType`, `SlotValue` |
| `Memory/` | `DynamicBufferAllocator`, `MeshGpuResources` (`MeshGpuRegistry`) |
| `Passes/` | `MainPassNode`, `ViewNode`, `TrackedRenderPass`, `RenderPassDescriptor`, `SwapchainTarget`, `ActiveSwapchainPass` |
| `Phases/` | `RenderPhase<T>`, `Opaque3dPhase`, `Transparent3dPhase`, phase items, draw functions, `QueueMeshPhaseItems` |
| `Pipelines/` | `PipelineCache`, `MeshPipeline`, `MeshPrepare`, ImGui pipeline/render node |
| `Shaders/` | Built-in GLSL shaders: `mesh.vert.glsl`, `mesh.frag.glsl`, `imgui.vert.glsl`, `imgui.frag.glsl` |

Top-level files: `Renderer.cs`, `RendererContext.cs`, `RendererDiagnostics.cs`, `RenderWorld.cs`, `RenderStage.cs`,
`RenderSurfaceInfo.cs`, `RenderTextures.cs`, `RenderTextureDesc.cs`, `GlslLoader.cs`.

### Editor Architecture

```
┌──────────────────────────────────────────────────────────────┐
│  SDL3 ENGINE WINDOW                                          │
│                                                              │
│  Vulkan scene render                                         │
│  ImGui debug overlays                                        │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  EMBEDDED WEBVIEW (Ultralight)                         │  │
│  │  Blazor Server UI (in-process) ◄► SignalR WebSocket    │  │
│  │  Hot-reloadable editor panels & inspectors             │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

**Single process:** The Blazor Server runs in-process on a background thread while SDL3/Vulkan drives the main thread.
The editor UI is rendered via an Ultralight WebView overlay composited into the Vulkan render pipeline.

**Hot-reloadable shells:** `ShellCompiler` watches a directory of `.cs`, `.razor`, and `.css` files. On change, it
recompiles via Roslyn and hot-swaps the resulting assembly into the `ShellRegistry`, making new editor panels and
inspectors available instantly without restarting.

### Logging

The engine uses a structured multi-provider logging system:

```csharp
// Create a category-scoped logger (static, one per class)
private static readonly ILogger Logger = Log.Category("MySystem");

// Or derive from a type name automatically
private static readonly ILogger Logger = Log.For<MyPlugin>();

// Log at different severity levels
Logger.Trace("Variable x = 42");
Logger.Debug("Processing batch of 100 entities.");
Logger.Info("System initialized.");
Logger.Warn("Resource missing - using defaults.");
Logger.Error("Operation failed.", exception);

// Frame-level trace (auto-suppressed after first few frames, then sampled)
Logger.FrameTrace($"Frame #{frameCount} begin");
```

Severity levels: `Trace` → `Debug` → `Info` → `Warning` → `Error` → `Critical`.

Providers:
- **Console** - ANSI-colored output with category and severity prefix.
- **File** - Persistent log written to `Engine.log` in the application directory. Initialized automatically on
  `App` construction via `FileLoggerProvider.Initialize(path)`.

### FAQ

**Static vs instance behavior methods?**
Static methods run once per stage invocation - ideal for global logic, UI overlays, or one-shot spawns. Instance methods
run per entity that has the behavior component and can read/write fields on `this`.

**Struct lifetimes and disposal?**
Structs are value types with no finalizer. If your behavior struct holds class references with unmanaged resources,
implement `IDisposable` on the struct. The ECS calls `Dispose()` for each component when an entity is despawned.

**Can I mix behaviors and manual systems?**
Yes. The behavior generator emits `SystemDescriptor` registrations under the hood. Hand-written systems registered via
`App.AddSystem` run alongside them in the same schedule.

**What happens if I don't declare `Read`/`Write` on a system?**
The parallel scheduler treats it conservatively as a broad writer, serializing it against all other systems in the same
stage. Adding explicit metadata enables parallel execution.

## Roadmap

### Implemented

- Platform layer (SDL3 windowing, input, timing)
- Vulkan device initialization, swapchain, VMA allocations
- `NullGraphicsDevice` for headless runs and unit tests
- Render pipeline (extract/prepare/queue/graph model) with mesh rendering
- Render components: Camera, Mesh, Material, Transform
- Render phases (opaque front-to-back, transparent back-to-front)
- GPU resource management: PipelineCache, DynamicBufferAllocator, MeshGpuRegistry
- SPIR-V shader compilation and reflection
- Asset pipeline: AssetServer, background loading, typed storage, handle deduplication, hot-reload
- Built-in asset loaders (ByteArray, String, GlslLoader)
- Sparse-set ECS with generational entity pool and change tracking
- Behavior source generator with filters, run conditions, toggle keys
- Parallel scheduler with resource-access batching
- ImGui runtime overlays
- WebView overlay (Ultralight + Vulkan compositing)
- Editor scaffolding (Blazor Server, hot-reloadable shells)
- Typed event system
- Structured logging (console + file)
- Unit test suite (Common, Entities, Graphics, Renderer, Editor)

### In Progress

- Material system with PBR shading and lighting
- Scene graph and serialization (USD integration)
- Editor tools: dockable panes, inspectors, scene view, property editors

### Planned

- Asset import loaders (Assimp: FBX, glTF, OBJ), texture loaders, asset packaging and caching
- Compute workloads (culling, particles, post-processing)
- Audio system
- Physics integration
- Networking
- Shader hot-reload
- CI/CD: cross-platform builds, automated formatting, linting
- Comprehensive documentation and samples

Items are aspirational and subject to change as the project evolves.

## Contributing

Early days! If you want to help:

- Try building and running on your platform and open issues for any rough edges.
- Propose small, well-scoped PRs (build scripts, docs, samples, or isolated subsystems).
- Keep changes platform-agnostic when possible.

By participating, you agree to abide by our [Code of Conduct](./.github/CODE_OF_CONDUCT.md).

## License

Code: MPL-2.0 license. See [LICENSE](./LICENSE) for the full text.

Contributions: By submitting a contribution, you agree to license your contribution under the same license as this
repository.
