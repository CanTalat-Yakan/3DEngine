using System.Runtime.InteropServices;

namespace Engine;

/// <summary>
/// Main render graph node that begins the swapchain render pass with <see cref="LoadOp.Clear"/>,
/// sets up the camera UBO, and draws all extracted mesh entities.
/// Subsequent nodes (webview, imgui) add ordering edges from <c>"main_pass"</c> to ensure
/// they run after the clear.
/// </summary>
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

        // ── Always begin swapchain render pass with Clear ────────────────
        var passDesc = new RenderPassDescriptor(
            swapchainTarget.RenderPass,
            swapchainTarget.Framebuffer,
            swapchainTarget.Extent,
            LoadOp.Clear,
            StoreOp.Store,
            clearColor);

        using var pass = renderContext.BeginTrackedRenderPass(passDesc);

        var extent = swapchainTarget.Extent;
        pass.SetViewport(0, 0, extent.Width, extent.Height, 0, 1);
        pass.SetScissor(0, 0, extent.Width, extent.Height);

        // ── Camera-dependent drawing ────────────────────────────────────
        var cameras = renderWorld.TryGet<RenderCameras>();
        if (cameras is null || cameras.Items.Count == 0)
            return; // Clear was already issued; no cameras means no 3D draws

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

