<p style="text-align:center">
  <img src="img/3DEngine_logo.png" alt="3D Engine logo" width="256"/>
</p>

<h1 style="text-align:center">3D Engine</h1>
<h4 style="text-align:center">C# 3D engine (Vulkan + SDL3). Editor planned with Avalonia UI.</h4>

<p style="text-align:center">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10.0-512BD4">
  <img alt="Graphics API" src="https://img.shields.io/badge/Graphics-Vulkan-AC162C">
  <img alt="Windowing" src="https://img.shields.io/badge/Windowing-SDL3-0B7BB2">
  <img alt="Status" src="https://img.shields.io/badge/Status-Early%20Preview-yellow">
  <img alt="Platforms" src="https://img.shields.io/badge/Platforms-Linux%20%7C%20Windows%20%7C%20macOS-lightgrey">
</p>

---

<!-- TOC -->
- [Overview](#overview)
- [Current Status](#current-status)
- [Tech Stack (in this repo)](#tech-stack-in-this-repo)
- [Supported Platforms](#supported-platforms)
- [Quickstart](#quickstart)
- [Minimal example (current state)](#minimal-example-current-state)
- [Project Layout](#project-layout)
- [Roadmap (high-level)](#roadmap-high-level)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)
<!-- /TOC -->

---

## Overview

This repository is the restart of a cross‑platform 3D engine written in C#. The runtime is being built on Vulkan for rendering and SDL3 for windowing/input. An editor built with Avalonia UI is planned but not implemented yet.

At the moment, the solution is intentionally minimal — a small SDL3 window loop to validate the toolchain and native dependencies. The goal is to iterate rapidly toward a modern, data‑driven engine and editor.

---

## Current Status

- Minimal SDL3 bootstrap that creates a window and clears the screen.
- Vulkan and related native dependencies are wired via NuGet (Vortice.* and SDL3‑CS bundles).
- No editor/UI, no gameplay framework yet. Public APIs are not stable.

See `Program.cs` for the current entry point and loop.

---

## Tech Stack (in this repo)

- SDL3 + SDL3‑CS: cross‑platform windowing, input, and basic rendering bootstrap.
- Vulkan via Vortice.Vulkan and VMA: modern, explicit GPU API and memory allocator.
- SPIR‑V toolchain (Vortice.SPIRV, SpirvCross): shader compilation and reflection.
- ImGui bundle: debug and tooling UI (runtime overlay; editor planned separately).
- AssimpNet: asset import for common 3D formats (planned integration).
- USD.NET / UniversalSceneDescription: scene and asset interchange (planned integration).
- Newtonsoft.Json: configuration and serialization utilities.

---

## Supported Platforms

- Linux: X11 and Wayland supported via SDL3. Ensure recent Vulkan drivers (Mesa/NVIDIA/AMD).
- Windows: Vulkan-capable GPU + drivers. No WinUI/WinAppSDK dependency; editor will use Avalonia.
- macOS: via Vulkan portability (MoltenVK) as available through dependencies. Status: experimental.

---

## Quickstart

Prerequisites:
- .NET SDK matching `TargetFramework` (see `Engine.csproj`, currently `net10.0`).
- A working Vulkan driver/runtime for your GPU. Validation layers recommended during development.

Build and run:

```bash
# Clone
git clone https://github.com/<your-account>/3DEngine.git
cd 3DEngine

# Restore, build, run (Debug)
dotnet restore
dotnet build -c Debug
dotnet run -c Debug
```

Notes:
- Native binaries for SDL3 and related libraries are bundled via NuGet. On Linux you still need standard system libs (X11/Wayland, audio, etc.) and up‑to‑date GPU drivers.
- If Vulkan initialization fails, verify your driver installation and ensure validation layers are either installed or disabled.

---

## Minimal example (current state)

This is essentially what `Program.cs` does today:

```csharp
using SDL3;

if (!SDL.Init(SDL.InitFlags.Video)) return;
if (!SDL.CreateWindowAndRenderer("SDL3 Create Window", 800, 600, 0, out var window, out var renderer)) return;
SDL.SetRenderDrawColor(renderer, 100, 149, 237, 0);
var running = true;
while (running)
{
    while (SDL.PollEvent(out var e))
        if ((SDL.EventType)e.Type == SDL.EventType.Quit) running = false;

    SDL.RenderClear(renderer);
    SDL.RenderPresent(renderer);
}
SDL.DestroyRenderer(renderer);
SDL.DestroyWindow(window);
SDL.Quit();
```

---

## Project Layout

- `3DEngine.sln` — Solution file.
- `Engine.csproj` — Project file targeting .NET 10.0 with Vulkan/SDL3 dependencies.
- `Program.cs` — Minimal SDL3 window loop entry point.
- `img/` — Branding assets (local only; no external image hotlinks).

---

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

---

## Troubleshooting

- SDL3 fails to load on Linux:
  - Ensure desktop packages for X11 or Wayland are installed (e.g., `libx11`, `wayland`, `xrandr`, audio backends).
  - On Wayland, SDL may fall back to X11; try setting `SDL_VIDEODRIVER=wayland` or `x11` to test.
- Vulkan initialization errors:
  - Confirm your GPU drivers are up to date and Vulkan runtime is present (`vulkaninfo` should work).
  - Install Vulkan validation layers for better diagnostics, or disable them if unavailable.
- macOS portability:
  - MoltenVK support may be required; check your Vulkan loader and SDK installation.

---

## Contributing

Early days! If you want to help:
- Try building/running on your platform and open issues for any rough edges.
- Propose small, well‑scoped PRs (build scripts, docs, samples, or isolated subsystems).
- Keep changes platform‑agnostic when possible.

A formal guideline will be added once the editor and initial subsystems land.

---

## License

The license will be finalized before the first tagged release. Until then, treat the code as work‑in‑progress with no implied warranty.
