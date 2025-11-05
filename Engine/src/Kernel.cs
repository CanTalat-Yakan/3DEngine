global using System;

namespace Engine;

public sealed partial class Kernel
{
    public static Kernel? Instance { get; private set; }

    public event Action? OnInitialize;
    public event Action? OnRender;
    public event Action? OnGUI;
    public event Action? OnDispose;

    public Config Config;

    public Kernel(Config config)
    {
        Config = config;
    }

    public void Initialize(nint windowHandle, (int W, int H) size, bool win32Window, string? assetsPath = null)
    {
        Instance ??= this;
        // Placeholder for renderer/device init. For now, nothing.
    }

    public void Frame()
    {
        OnInitialize?.Invoke();
        OnInitialize = null;

        OnRender?.Invoke();
        OnGUI?.Invoke();
    }

    public void Dispose()
    {
        OnDispose?.Invoke();
        GC.SuppressFinalize(this);
    }
}

public sealed partial class Kernel
{
    public void BeginRender() { }
    public void EndRender() { }
    public void RenderGUI() { }
    public void Compile() { }
}

