<p align="center" style="text-align:center">
  <img src="Images/3DEngine_Icon.png" alt="3D Engine Icon" width="256"/>
</p>

<h1 align="center" style="text-align:center">3D Engine</h1>
<h4 align="center" style="text-align:center">C# 3D Game Engine (Vulkan + SDL3).</h4>
<p align="center" style="text-align:center">Editor planned with Avalonia UI.</p>

<p align="center" style="text-align:center">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10.0-512BD4">
  <img alt="Graphics API" src="https://img.shields.io/badge/Graphics-Vulkan-AC162C">
  <img alt="Windowing" src="https://img.shields.io/badge/Windowing-SDL3-0B7BB2">
</p>

<p align="center" style="text-align:center">
  <img alt="Status" src="https://img.shields.io/badge/Status-Early%20Preview-yellow">
  <img alt="Platforms" src="https://img.shields.io/badge/Platforms-Linux%20%7C%20Windows%20%7C%20macOS-lightgrey">
</p>

<!-- TOC -->

- [Overview](#overview)
- [Design Goals](#design-goals)
- [Current Status](#current-status)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Supported Platforms](#supported-platforms)
- [Build and Run](#build-and-run)
- [Quick Start](#quick-start)
- [How It Works](#how-it-works)
    - [Stages and the Schedule](#stages-and-the-schedule)
    - [Plugins](#plugins)
    - [Resources](#resources)
    - [ECS: Entities, Components, and Commands](#ecs-entities-components-and-commands)
    - [Behavior System (Attribute-based ECS)](#behavior-system-attribute-based-ecs)
    - [Native ECS Style (Manual Systems)](#native-ecs-style-manual-systems)
    - [Source Generator](#source-generator)
    - [FAQ](#faq)
- [Roadmap (high-level)](#roadmap-high-level)
- [Contributing](#contributing)
- [License](#license)

<!-- /TOC -->

## Overview

This repository is the restart of a cross‑platform 3D engine written in C#. The runtime is being built on Vulkan for
rendering and SDL3 for windowing/input. An editor built with Avalonia UI is planned but not implemented yet.

The runtime already includes a minimal ECS and a behavior system powered by a Roslyn source generator. You can author
gameplay logic in two ways:

- Behavior system (attribute-based): mark a struct with `[Behavior]`, add methods with stage attributes like
  `[OnUpdate]`, and the generator wires them into the schedule.
- Native ECS style: manually add systems to stages and work with `ECSWorld` queries and `ECSCommands`.

## Design Goals

- Ergonomic ECS: Lightweight world + staged schedule inspired by Bevy; attribute-driven behaviors to reduce boilerplate.
- Explicit Rendering: Vulkan-first with a modern resource and synchronization model; avoid hidden abstractions.
- Extensibility: Plugins compose engine services; source generation handles repetitive registration tasks.
- Cross‑Platform: Linux, Windows, macOS (MoltenVK) targeted with minimal platform-specific code in user land.
- Separation of Concerns: Runtime (game loop, ECS, renderer) separate from future editor (Avalonia UI) tooling layer.
- Fast Iteration: Hot‑reload ambitions for shaders/assets; clear, inspectable runtime overlay via ImGui.

## Current Status

- SDL3 bootstrap that creates a window and drives a staged update loop.
- Vulkan and related native dependencies are wired via NuGet (Vortice.* and SDL3‑CS bundles).
- ECS core (entities/components, simple queries) and a behavior source generator that auto-registers systems.
- ImGui runtime overlay is available for diagnostics.
- APIs are evolving; breaking changes are expected.

See `Engine/Program.cs` for usage examples.

## Features

Implemented (early preview):
- Staged update loop (Startup → First → PreUpdate → Update → PostUpdate → Render → Last)
- Minimal ECS (entities, components, queries, changed flags, disposal semantics)
- Attribute-based behavior system with Roslyn source generator
- Basic plugin model (`DefaultPlugins`) for window/time/input/ECS/ImGui
- ImGui runtime overlay integration
- Vulkan device + window initialization scaffolding (rendering pipeline WIP)

In Progress / Planned (see roadmap for detail):
- Renderer: swapchain, command buffers, descriptor sets, shader reflection
- Asset pipeline: import, packaging, caching
- Scene graph & serialization (USD integration)
- Editor (Avalonia) with dockable tools & inspectors
- Job system & parallelism

## Tech Stack

- SDL3 + SDL3‑CS: cross‑platform windowing, input, and basic rendering bootstrap.
- Vulkan via Vortice.Vulkan and VMA: modern, explicit GPU API and memory allocator.
- SPIR‑V toolchain (Vortice.SPIRV, SpirvCross): shader compilation and reflection.
- ImGui bundle: debug and tooling UI (runtime overlay; editor planned separately).
- AssimpNet: asset import for common 3D formats (planned integration).
- USD.NET / UniversalSceneDescription: scene and asset interchange (planned integration).
- Newtonsoft.Json: configuration and serialization utilities.

## Supported Platforms

- Linux: X11 and Wayland supported via SDL3. Ensure recent Vulkan drivers (Mesa/NVIDIA/AMD).
- Windows: Vulkan-capable GPU + drivers. No WinUI/WinAppSDK dependency; editor will use Avalonia.
- macOS: via Vulkan portability (MoltenVK) as available through dependencies. Status: experimental.

## Build and Run

Prerequisites:

- .NET SDK matching `TargetFramework` (see `Engine/Engine.csproj`, currently `net10.0`). Preview SDK may be required.
- A working Vulkan driver/runtime for your GPU.

Clone + build:

```bash
# Clone
git clone https://github.com/CanTalat-Yakan/3DEngine.git
cd 3DEngine

# Restore & build all projects
dotnet build

# Run the engine sample (runtime entry point)
dotnet run --project Engine
```

Open in an IDE (Rider/VS/VSCode) using `3DEngine.sln` if you prefer.

Notes:

- Native binaries for SDL3 and related libraries are bundled via NuGet. On Linux you still need standard system libs (
  X11/Wayland, audio, etc.) and up‑to‑date GPU drivers.
- If Vulkan initialization fails, verify your driver installation and ensure validation layers are either installed or
  disabled.

## Quick Start

An automatic minimal behavior + system example:

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
        Angle += (float)ctx.Res<Time>().DeltaSeconds * 90f; // 90 deg/s
        Console.WriteLine($"Entity {ctx.EntityID} angle now {Angle:0.00}");
    }
}
```

1. Add the struct in any runtime project.
2. Build: the source generator emits systems automatically.
3. Run: the console prints per-frame updates from the behavior.

To add a manual system instead:

```csharp
public sealed class Program
{
    [STAThread]
    private static void Main()
    {
        new App(Config.GetDefault())
            .AddPlugin(new DefaultPlugins())
            .AddPlugin(new SamplePlugin())
            .Run();
    }
}

public sealed class SamplePlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddSystem(Stage.Startup, (world) =>
        {
            var ecs = world.Resource<EcsWorld>();
            var e = ecs.Spawn();
            ecs.Add(e, new Spinner { Angle = 0f });
        });
        
        app.AddSystem(Stage.Update, (world) =>
        {
            var ecs = world.Resource<EcsWorld>();
            foreach (var (e, spinner) in ecs.Query<Spinner>())
            {
                var newSpinner = spinner;
                newSpinner.Angle += (float)world.Resource<Time>().DeltaSeconds * 45f;
                ecs.Update(e, newSpinner);
            }
        });
    }
}
```

## How It Works

### Stages and the Schedule

The engine drives a Bevy-like staged loop (`Source/App/Stage.cs`):

- `Startup` (once), then per frame: `First` → `PreUpdate` → `Update` → `PostUpdate` → `Render` → `Last`.
- Systems are `SystemFn(World world)` delegates added to stages via `App.AddSystem(stage, system)` and executed by
  `Schedule`.

### Plugins

Plugins configure the app and register systems. `DefaultPlugins` wires everything you typically need:

- Window, time, input, events
- ECS world/commands and a post-update command application pass
- Auto-registration of generated behavior systems (see below)
- Kernel/ImGui setup and a clear-color system

### Resources

`World` is a simple resource container (Bevy-style). Insert and fetch singletons by type:

- `app.InsertResource(new MyService())` or `world.InsertResource(value)`
- `world.Resource<T>()` to retrieve; throws if missing
- `BehaviorContext.Res<T>()` is a shortcut for `world.Resource<T>()`

Common resources used by systems/behaviors:

- `EcsWorld` – entity/component storage and queries
- `EcsCommands` – queued mutations applied after `Update` (at `PostUpdate`)
- `AppWindow`, `Time`, `GUIRenderer`, etc.

### ECS: Entities, Components, and Commands

- Entities are `int` IDs managed by `EcsWorld`.
- Components are plain structs or classes stored by type.
- Mutations can be immediate (`EcsWorld.Add/Update/Despawn`) or queued via `ECSCommands` to avoid in-frame structural
  changes:
    - Queue with `ctx.Cmd.Add(...)`, `ctx.Cmd.Spawn(...)`, etc.
    - Applied automatically in `PostUpdate` by the `EcsPlugin`.
- Queries:
    - `foreach (var (entity, comp) in ecs.Query<T>()) { ... }`
    - `Query<T1,T2>()` and `Query<T1,T2,T3>()` exist for small joins.
    - `Has<T>(entity)`, `Changed<T>(entity)` helpers are available.
- Note: Per-frame `Changed<...>` flags are cleared at stage `First`.
- Disposal: When an entity is despawned, any component that implements `IDisposable` is disposed automatically. Prefer
  implementing `IDisposable` on components (including behavior structs) if they hold disposable references, and release
  them in `Dispose()`.

### Behavior System (Attribute-based ECS)

Author gameplay in a script-like way:

1) Mark a struct with `[Behavior]`.
2) Add methods and mark when they should run using attributes:
    - `[OnStartup]`, `[OnFirst]`, `[OnPreUpdate]`, `[OnUpdate]`, `[OnPostUpdate]`, `[OnRender]`, `[OnLast]`.
3) Optionally add filters on instance methods:
    - `[With(typeof(Position), typeof(Velocity))]`
    - `[Without(typeof(Disabled))]`
    - `[Changed(typeof(Transform))]`

- Note: The current generator supports `With` joins of up to two component types. If more are specified, it falls back
  to querying only the behavior component and applies `Without`/`Changed` checks inside the loop.

Static vs Instance methods:

- Static methods run once per stage invocation and receive `BehaviorContext`.
- Instance methods run per entity that has this behavior component. They can use fields/properties on `this`. The
  generator:
    - Iterates `ecs.Query<YourBehavior>()`
    - Sets `ctx.EntityID`
    - Calls your method
    - Writes back the component with `ecs.Update`

Creating entities for instance behaviors:

- Instance methods only run if at least one entity has that behavior. A common pattern is a static `[OnStartup]` to
  spawn and add the behavior component.

Access to engine services:

- Use `ctx.Res<T>()` for other resources (e.g., `Time`, `Input`, etc.).
- `ctx.Ecs` and `ctx.Cmd` provide ECS access.

Reference types inside behavior structs:

- Safe and supported. Storing a class reference in your behavior struct allows complex per-entity state without copying
  large data. Initialize lazily or in `[OnStartup]` as needed.

Examples:

```csharp
using ImGuiNET;

[Behavior]
public struct HUDOverlay
{
    [OnUpdate]
    public static void Draw(BehaviorContext ctx)
    {
        ImGui.Begin("HUD");
        ImGui.Text($"FPS: {(1.0 / ctx.Res<Time>().DeltaSeconds):0}");
        ImGui.End();
    }
}

[Behavior]
public struct Spawner
{
    public float a;
    private float b { get; set; }

    [OnStartup]
    public static void Init(BehaviorContext ctx)
    {
        var e = ctx.Ecs.Spawn();
        ctx.Ecs.Add(e, new Spawner { a = 1.0f });
    }

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        b += (float)ctx.Res<Time>().DeltaSeconds;
        Console.WriteLine($"Spawner running. a={a}, b={b}");
    }
}

public class SomeDisposable : IDisposable
{
    private float _num = 2;

    public string Log() => _num.ToString();
    
    public void Dispose()
    {
        // Cleanup resources
    }
}

[Behavior]
public struct HeavyBehavior : IDisposable
{
    private SomeDisposable _handle;

    [OnStartup]
    public static void Init(BehaviorContext ctx)
    {
        var e = ctx.Ecs.Spawn();
        ctx.Ecs.Add(e, new HeavyBehavior { _handle = new SomeDisposable() });
    }

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Console.WriteLine(_handle.Log());
    }

    public void Dispose()
    {
        _handle?.Dispose();
        _handle = null;
    }
}
```

### Native ECS Style (Manual Systems)

Prefer writing systems directly? Use `App.AddSystem` and operate on `EcsWorld`:

```csharp
app.AddSystem(Stage.Update, (World w) =>
{
    var ecs = w.Resource<EcsWorld>();
    foreach (var (e, comp) in ecs.Query<MyComponent>())
    {
        // mutate comp and write back
        ecs.Update(e, comp);
    }
});
```

You can mix and match: the behavior generator emits systems under the hood; you can still register hand-written systems
alongside them.

### Source Generator

`Engine.SourceGen` scans for `[Behavior]` structs and methods with stage attributes, then emits:

- Per-behavior static classes with stage entry points that call your methods (static or per-entity loops for instance
  methods).
- A `BehaviorsPlugin` that registers those systems into the app.

`DefaultPlugins` includes `BehaviorsPlugin`, so behaviors are picked up automatically at build time—no manual
registration required.

### FAQ

- Static vs Instance: Static methods run once per stage and are great for global logic/UI; instance methods run per
  entity and can use fields/properties on the component.
  structs.
- Struct lifetimes and disposal: Structs are value types; they aren't “destroyed” with a finalizer. If your struct holds
  class references with unmanaged resources, implement `IDisposable` on the struct and dispose those references in
  `Dispose()`. The ECS will invoke `Dispose()` for components on `Despawn`.

## Roadmap (high-level)

- Core
    - Robust platform layer (windowing, input, timing) on SDL3
    - Vulkan renderer: swapchain, command submission, synchronization, VMA allocations
    - Shader pipeline: SPIR‑V compilation, reflection, hot‑reload
    - Asset pipeline: import (Assimp), packaging, and caching
    - Scene graph and serialization (USD integration)
    - ECS and job system
- Tooling
    - Editor built with Avalonia UI (dockable panes, inspectors, scene view)
    - ImGui runtime overlay for debugging
    - Live reload for assets and scripts
- Systems
    - Material system and PBR
    - Compute workloads (culling, particles, post‑processing)
    - Audio, physics, and networking (research and vendor selection TBD)
- CI/DevX
    - Cross‑platform builds (Linux/Windows/macOS)
    - Automated formatting, linting, and basic tests

Items are aspirational and subject to change as the project evolves.

## Contributing

Early days! If you want to help:

- Try building/running on your platform and open issues for any rough edges.
- Propose small, well‑scoped PRs (build scripts, docs, samples, or isolated subsystems).
- Keep changes platform‑agnostic when possible.

By participating, you agree to abide by our [Code of Conduct](./CODE_OF_CONDUCT.md).

A formal guideline will be added once the editor and initial subsystems land.

## License

Code: MIT license. See [LICENSE](./LICENSE) for the full text.

Contributions: By submitting a contribution, you agree to license your contribution under the same license as this
repository.
