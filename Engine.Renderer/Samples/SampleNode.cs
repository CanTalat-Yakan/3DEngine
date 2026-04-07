namespace Engine;

/// <summary>Sample render graph node that consumes camera descriptor data.</summary>
/// <remarks>
/// Acts as a dependency anchor in the render graph - other nodes (e.g. ImGui, WebView)
/// declare a dependency on <c>"sample"</c> to ensure they render <em>after</em> the 3D scene.
/// This minimal implementation is a placeholder; a real scene would issue draw calls here.
/// </remarks>
/// <seealso cref="SampleExtract"/>
/// <seealso cref="SamplePrepare"/>
/// <seealso cref="SampleQueue"/>
public sealed class SampleNode : IRenderNode
{
    /// <inheritdoc />
    public string Name => "sample";
    /// <inheritdoc />
    public IReadOnlyCollection<string> Dependencies => Array.Empty<string>();

    /// <inheritdoc />
    public void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld)
    {
        _ = ctx;
        _ = cmds;

        var cameraSet = renderWorld.TryGet<IDescriptorSet>();
        _ = cameraSet;
    }
}
