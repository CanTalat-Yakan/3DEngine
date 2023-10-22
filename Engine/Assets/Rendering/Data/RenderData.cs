using System.Drawing;

using Vortice.Direct3D12;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.D3DCompiler;
using System.Threading;

namespace Engine.Data;

public struct RenderData
{
    public IDXGISwapChain3 SwapChain;

    public ID3D12GraphicsCommandList4 CommandList;
    public ID3D12CommandQueue GraphicsQueue;
    public ID3D12CommandAllocator[] CommandAllocators;

    public ID3D12Fence FrameFence;
    public AutoResetEvent FrameFenceEvent;

    public ID3D12Resource BackBufferRenderTargetTexture;
    public ID3D12DescriptorHeap BackBufferRenderTargetView;

    public ID3D12Resource MSAARenderTargetTexture;
    public ID3D12DescriptorHeap MSAARenderTargetView;

    public ID3D12Resource DepthStencilTexture;
    public ID3D12DescriptorHeap DepthStencilView;

    public BlendDescription BlendState;

    public RasterizerDescription RasterizerState;

    public PrimitiveTopology PrimitiveTopology;

    public const Format RenderTargetFormat = Format.R8G8B8A8_UNorm; // 10 bits = Format.R10G10B10A2_UNorm, 16 bits = Format.R16G16B16A16_UNorm
    public const Format DepthStencilFormat = Format.D32_Float;
    public const int RenderLatency = 2;

    public int BackBufferIndex;

    public ulong FrameCount;
    public ulong FrameIndex;

    public void SetRasterizerDescFillModeWireframe() =>
        SetRasterizerDescFillMode(FillMode.Wireframe);

    public void SetRasterizerDescFillMode(FillMode fillMode = FillMode.Solid)
    {
        RasterizerState.FillMode = fillMode;
        RasterizerState.CullMode = fillMode == FillMode.Solid ? CullMode.Back : CullMode.None;
        RasterizerState.FrontCounterClockwise = true;
    }

    public void SetViewport(Size size) =>
        CommandList.RSSetViewport(new Viewport(0, 0, size.Width, size.Height, 0.0f, 1.0f));

    public void SetupInputAssembler(IndexBufferView indexBufferView, params VertexBufferView[] vertexBufferViews)
    {
        CommandList.IASetVertexBuffers(0, vertexBufferViews);
        CommandList.IASetIndexBuffer(indexBufferView);
        CommandList.IASetPrimitiveTopology(PrimitiveTopology);
    }

    public void SetupMaterial(ID3D12PipelineState pipelineState) =>
        CommandList.SetPipelineState(pipelineState);

    public void Dispose()
    {
        SwapChain?.Dispose();

        CommandList?.Dispose();
        GraphicsQueue?.Dispose();
        foreach (var commandAllocator in CommandAllocators)
            commandAllocator.Dispose();

        FrameFence?.Dispose();
        FrameFenceEvent?.Dispose();

        BackBufferRenderTargetView?.Dispose();
        MSAARenderTargetView?.Dispose();
        DepthStencilView?.Dispose();

        DisposeTexturesAndViews();
    }

    public void DisposeTexturesAndViews()
    {
        BackBufferRenderTargetTexture?.Dispose();
        MSAARenderTargetTexture?.Dispose();
        DepthStencilTexture?.Dispose();
    }

    public static ReadOnlyMemory<byte> CompileBytecode(string filePath, string entryPoint, string profile)
    {
        // Shader flags to enable strictness and set optimization level or debug mode.
        ShaderFlags shaderFlags = ShaderFlags.EnableStrictness;
#if DEBUG
        shaderFlags |= ShaderFlags.Debug;
        shaderFlags |= ShaderFlags.SkipValidation;
#else
        shaderFlags |= ShaderFlags.OptimizationLevel3;
#endif

        // Compile the shader from the specified file using the specified entry point, profile, and flags.
        return Compiler.CompileFromFile(filePath, entryPoint, profile, shaderFlags);
    }
}
