namespace Engine;

/// <summary>
/// Selects the Ultralight rendering backend for the webview.
/// </summary>
/// <seealso cref="WebViewPlugin"/>
/// <seealso cref="WebViewInstance"/>
public enum WebViewMode
{
    /// <summary>
    /// CPU bitmap surface mode. Ultralight renders to a CPU-side pixel buffer
    /// which is then uploaded to a Vulkan texture each frame. Simple and robust, but
    /// incurs a per-frame CPU→GPU copy.
    /// </summary>
    Cpu = 0,

    /// <summary>
    /// GPU-accelerated mode (default). A custom <c>IGpuDriver</c> implementation translates
    /// Ultralight draw commands directly into Vulkan calls, rendering into GPU textures
    /// with no CPU pixel readback. Higher performance for complex web content.
    /// </summary>
    Gpu = 1,
}

