using System.Drawing;
using System.IO;
using System.Threading;

using Vortice.D3DCompiler;
using Vortice.Direct3D12;
using Vortice.Direct3D;
using Vortice.Dxc;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Data;

public struct RenderData
{
    public Material Material;

    public IDXGISwapChain3 SwapChain;

    public ID3D12CommandQueue GraphicsQueue;

    public ID3D12Fence FrameFence;
    public AutoResetEvent FrameFenceEvent;

    public ID3D12Resource BackBufferRenderTargetTexture => BufferRenderTargetTextures[SwapChain.CurrentBackBufferIndex];
    public ID3D12Resource[] BufferRenderTargetTextures;
    public ID3D12DescriptorHeap BufferRenderTargetView;

    public ID3D12Resource MSAARenderTargetTexture;
    public ID3D12DescriptorHeap MSAARenderTargetView;

    public ID3D12Resource DepthStencilTexture;
    public ID3D12DescriptorHeap DepthStencilView;

    public RasterizerDescription RasterizerDescription;
    public BlendDescription BlendDescription;

    public PrimitiveTopology PrimitiveTopology;

    public const Format RenderTargetFormat = Format.R8G8B8A8_UNorm; // 10 bits = Format.R10G10B10A2_UNorm, 16 bits = Format.R16G16B16A16_UNorm
    public const Format DepthStencilFormat = Format.D32_Float;
    public const int RenderLatency = 2;

    public ulong FrameIndex => FrameCount % RenderLatency;
    public ulong FrameCount;

    public void SetViewport(Size size)
    {
        Material.CommandList.RSSetViewport(new Viewport(size.Width, size.Height));
        Material.CommandList.RSSetScissorRect(size.Width, size.Height);
    }

    public void SetupInputAssembler(IndexBufferView indexBufferView, params VertexBufferView[] vertexBufferViews)
    {
        Material.CommandList.IASetVertexBuffers(0, vertexBufferViews);
        Material.CommandList.IASetIndexBuffer(indexBufferView);
        Material.CommandList.IASetPrimitiveTopology(PrimitiveTopology);
    }

    public void Dispose()
    {
        Material?.Dispose();

        SwapChain?.Dispose();

        GraphicsQueue?.Dispose();

        FrameFence?.Dispose();
        FrameFenceEvent?.Dispose();

        DisposeTexturesAndViews();
    }

    public void DisposeTexturesAndViews()
    {
        foreach (var bufferRenderTargetTextures in BufferRenderTargetTextures)
            bufferRenderTargetTextures?.Dispose();
        BufferRenderTargetView?.Dispose();

        MSAARenderTargetView?.Dispose();
        MSAARenderTargetTexture?.Dispose();

        DepthStencilTexture?.Dispose();
        DepthStencilView?.Dispose();
    }

    public static ReadOnlyMemory<byte> CompileBytecode(DxcShaderStage stage, string filePath, string entryPoint)
    {
        string directory = Path.GetDirectoryName(filePath);
        string shaderSource = File.ReadAllText(filePath);

        using (ShaderIncludeHandler includeHandler = new(Paths.SHADERS, directory))
        {
            using IDxcResult results = DxcCompiler.Compile(stage, shaderSource, entryPoint, includeHandler: includeHandler);
            if (results.GetStatus().Failure)
                throw new Exception(results.GetErrors());

            return results.GetObjectBytecodeMemory();
        }
    }
}