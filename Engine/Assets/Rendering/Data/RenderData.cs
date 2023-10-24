using System.Drawing;

using Vortice.Direct3D12;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.D3DCompiler;
using System.Threading;
using System.IO;
using Vortice.Dxc;

namespace Engine.Data;

public struct RenderData
{
    public IDXGISwapChain3 SwapChain;

    public ID3D12CommandQueue GraphicsQueue;

    public Material Material;

    public ID3D12Fence FrameFence;
    public AutoResetEvent FrameFenceEvent;

    public ID3D12Resource BackBufferRenderTargetTexture => BufferRenderTargetTextures[SwapChain.CurrentBackBufferIndex];
    public ID3D12Resource[] BufferRenderTargetTextures;
    public ID3D12DescriptorHeap BufferRenderTargetView;

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

    public ulong FrameIndex => FrameCount % RenderLatency;
    public ulong FrameCount;

    public void SetRasterizerDescFillModeWireframe() =>
        SetRasterizerDescFillMode(FillMode.Wireframe);

    public void SetRasterizerDescFillMode(FillMode fillMode = FillMode.Solid)
    {
        RasterizerState.FillMode = fillMode;
        RasterizerState.CullMode = fillMode == FillMode.Solid ? CullMode.Back : CullMode.None;
        RasterizerState.FrontCounterClockwise = true;
    }

    public void SetViewport(Size size) =>
        Material.CommandList.RSSetViewport(new Viewport(0, 0, size.Width, size.Height, 0.0f, 1.0f));

    public void SetupInputAssembler(IndexBufferView indexBufferView, params VertexBufferView[] vertexBufferViews)
    {
        Material.CommandList.IASetVertexBuffers(0, vertexBufferViews);
        Material.CommandList.IASetIndexBuffer(indexBufferView);
        Material.CommandList.IASetPrimitiveTopology(PrimitiveTopology);
    }

    public void Dispose()
    {
        SwapChain?.Dispose();

        GraphicsQueue?.Dispose();

        Material?.Dispose();

        FrameFence?.Dispose();
        FrameFenceEvent?.Dispose();

        BufferRenderTargetView?.Dispose();
        MSAARenderTargetView?.Dispose();
        DepthStencilView?.Dispose();

        DisposeTexturesAndViews();
    }

    public void DisposeTexturesAndViews()
    {
        foreach (var bufferRenderTargetTextures in BufferRenderTargetTextures)
            bufferRenderTargetTextures?.Dispose();
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
