namespace Engine;

/// <summary>
/// A render graph node with typed input/output slots, matching Bevy's Node trait.
/// Nodes own their own render passes and declare resource dependencies via slots.
/// </summary>
/// <seealso cref="ViewNode"/>
/// <seealso cref="RenderGraph"/>
public interface INode
{
    /// <summary>Declares the node's input slots (data it consumes from upstream nodes).</summary>
    SlotInfo[] Input() => Array.Empty<SlotInfo>();

    /// <summary>Declares the node's output slots (data it produces for downstream nodes).</summary>
    SlotInfo[] Output() => Array.Empty<SlotInfo>();

    /// <summary>Called before Run() to prepare GPU resources. Receives the render world for resource access.</summary>
    /// <param name="renderWorld">The render world containing render resources.</param>
    void Update(RenderWorld renderWorld) { }

    /// <summary>Executes the node's rendering logic - creating render passes, binding pipelines, issuing draw calls.</summary>
    /// <param name="graphContext">Context for accessing slot values and running sub-graphs.</param>
    /// <param name="renderContext">Context wrapping the graphics device, command buffer, and dynamic allocator.</param>
    /// <param name="renderWorld">The render world containing render resources.</param>
    void Run(RenderGraphContext graphContext, RenderContext renderContext, RenderWorld renderWorld);
}

/// <summary>
/// Convenience base that auto-iterates <see cref="RenderCameras"/>, calling
/// <see cref="Run(RenderGraphContext, RenderContext, RenderCamera, RenderWorld)"/> per camera.
/// Matches Bevy's ViewNode pattern using <see cref="RenderCameras"/> as the view query type.
/// </summary>
/// <seealso cref="INode"/>
/// <seealso cref="RenderCameras"/>
public abstract class ViewNode : INode
{
    /// <inheritdoc />
    public virtual SlotInfo[] Input() => Array.Empty<SlotInfo>();

    /// <inheritdoc />
    public virtual SlotInfo[] Output() => Array.Empty<SlotInfo>();

    /// <inheritdoc />
    public virtual void Update(RenderWorld renderWorld) { }

    /// <summary>Iterates all cameras and calls the per-camera Run overload.</summary>
    public void Run(RenderGraphContext graphContext, RenderContext renderContext, RenderWorld renderWorld)
    {
        var cameras = renderWorld.TryGet<RenderCameras>();
        if (cameras is null) return;

        foreach (var camera in cameras.Items)
            Run(graphContext, renderContext, camera, renderWorld);
    }

    /// <summary>Executes rendering logic for a single camera view.</summary>
    /// <param name="graphContext">Context for accessing slot values and running sub-graphs.</param>
    /// <param name="renderContext">Context wrapping the graphics device and command buffer.</param>
    /// <param name="camera">The camera to render from.</param>
    /// <param name="renderWorld">The render world containing render resources.</param>
    protected abstract void Run(RenderGraphContext graphContext, RenderContext renderContext, RenderCamera camera, RenderWorld renderWorld);
}

