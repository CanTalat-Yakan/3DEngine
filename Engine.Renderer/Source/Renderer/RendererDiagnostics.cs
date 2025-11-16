namespace Engine;

/// <summary>
/// Diagnostic snapshot summarizing adapter, surface, and frame metrics for the renderer.
/// </summary>
public sealed class RendererDiagnostics
{
    public GraphicsAdapterInfo AdapterInfo { get; private set; } = GraphicsAdapterInfo.Unknown;
    public Extent2D SurfaceExtent { get; private set; }
    public ulong SurfaceRevision { get; private set; }
    public ulong FramesRendered { get; private set; }

    internal void Initialize(GraphicsAdapterInfo adapter)
    {
        AdapterInfo = adapter;
    }

    internal void RecordFrame(GraphicsAdapterInfo adapter, Extent2D extent, ulong surfaceRevision)
    {
        AdapterInfo = adapter;
        SurfaceExtent = extent;
        SurfaceRevision = surfaceRevision;
        FramesRendered++;
    }
}
