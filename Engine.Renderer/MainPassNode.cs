using System.Runtime.InteropServices;

namespace Engine;

/// <summary>
/// Main render graph node that begins the swapchain render pass with <see cref="LoadOp.Clear"/>,
/// sets up the camera UBO, and draws all extracted mesh entities.
/// The render pass is left OPEN and published as <see cref="ActiveSwapchainPass"/> in the
/// <see cref="RenderWorld"/> so downstream overlay nodes (webview, imgui) can draw into the
/// same pass without the overhead of separate begin/end cycles.
/// <see cref="Renderer"/> ends the pass after all graph nodes have executed.
/// </summary>
/// <seealso cref="ActiveSwapchainPass"/>
/// <seealso cref="MeshPipeline"/>
/// <seealso cref="CameraExtract"/>
public sealed class MainPassNode : INode, IDisposable
{
    private IDescriptorSet? _cameraSet;
    private MeshPipeline? _meshPipeline;

    /// <inheritdoc />
    public void Run(RenderGraphContext graphContext, RenderContext renderContext, RenderWorld renderWorld)
    {
        var gfx = renderContext.Device;
        var swapchainTarget = renderWorld.TryGet<SwapchainTarget>();
        if (swapchainTarget is null) return;

        var clearColor = renderWorld.TryGet<ClearColor>() is { } cc ? cc : ClearColor.Black;

        // ── Begin swapchain render pass with Clear ──────────────────────
        // The pass is NOT disposed here - it stays open for overlay nodes.
        // Renderer.ExecuteGraph closes it after all nodes have run.
        var passDesc = new RenderPassDescriptor(
            swapchainTarget.RenderPass,
            swapchainTarget.Framebuffer,
            swapchainTarget.Extent,
            LoadOp.Clear,
            StoreOp.Store,
            clearColor);

        var pass = renderContext.BeginTrackedRenderPass(passDesc);

        var extent = swapchainTarget.Extent;
        pass.SetViewport(0, 0, extent.Width, extent.Height, 0, 1);
        pass.SetScissor(0, 0, extent.Width, extent.Height);

        // Publish the open pass for downstream overlay nodes
        renderWorld.Set(new ActiveSwapchainPass(pass, extent));

        // ── Camera-dependent drawing ────────────────────────────────────
        var cameras = renderWorld.TryGet<RenderCameras>();
        if (cameras is null || cameras.Items.Count == 0)
            return; // Clear was already issued; overlay nodes will still draw

        var camera = cameras.Items[0];

        // ── Prepare: camera UBO ─────────────────────────────────────────
        var allocator = renderContext.DynamicAllocator;
        if (allocator is not null)
        {
            var uniform = new CameraUniform
            {
                View = camera.View,
                Projection = camera.Projection
            };

            var sizeBytes = (ulong)Marshal.SizeOf<CameraUniform>();
            var alloc = allocator.Allocate(sizeBytes, BufferUsage.Uniform);

            var span = allocator.Map(alloc);
            MemoryMarshal.Write(span, in uniform);
            allocator.Unmap(alloc);

            _cameraSet ??= gfx.CreateDescriptorSet();

            var uboBinding = new UniformBufferBinding(alloc.Buffer, 0, alloc.Offset, sizeBytes);
            gfx.UpdateDescriptorSet(_cameraSet, uboBinding, samplerBinding: null);
        }

        if (_cameraSet is null) return;

        // ── Mesh draw ───────────────────────────────────────────────────
        var extracted = renderWorld.TryGet<ExtractedMeshData>();
        if (extracted is null || extracted.Entries.Count == 0)
            return;

        var registry = renderWorld.TryGet<MeshGpuRegistry>();
        if (registry is null)
            return;

        if (_meshPipeline is null)
        {
            MeshShaders.EnsureLoaded();
            _meshPipeline = new MeshPipeline(gfx, swapchainTarget.RenderPass,
                MeshShaders.Vertex, MeshShaders.Fragment);
        }

        pass.SetPipeline(_meshPipeline.Pipeline);
        pass.SetBindGroup(_meshPipeline.Pipeline, _cameraSet);

        foreach (var entry in extracted.Entries)
        {
            if (!registry.TryGet(entry.EntityId, out var vertexBuffer))
                continue;

            var pushConstants = new MeshPushConstants
            {
                Model = entry.ModelMatrix,
                Albedo = entry.Albedo
            };

            pass.PushConstants(_meshPipeline.Pipeline, ShaderStageFlags.All,
                0, MemoryMarshal.AsBytes(new ReadOnlySpan<MeshPushConstants>(in pushConstants)));

            pass.SetVertexBuffer(0, new[] { vertexBuffer }, new ulong[] { 0 });
            pass.Draw((uint)entry.VertexCount);
        }
    }

    /// <summary>Disposes the persistent camera descriptor set.</summary>
    public void Dispose()
    {
        _cameraSet?.Dispose();
    }
}

