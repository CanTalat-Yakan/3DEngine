using System.Collections.Generic;

using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Engine.DataStructures;

public sealed class PipelineStateObject : IDisposable
{
    public List<PipelineStateObjectBundle> PipelineStateObjectBundles = new();

    public ReadOnlyMemory<byte> VertexShader;
    public ReadOnlyMemory<byte> GeometryShader;
    public ReadOnlyMemory<byte> PixelShader;

    public string Name;

    public PipelineStateObject(ReadOnlyMemory<byte> vertexShader, ReadOnlyMemory<byte> pixelShader)
    {
        VertexShader = vertexShader;
        PixelShader = pixelShader;
    }

    public ID3D12PipelineState GetState(GraphicsDevice device, PipelineStateObjectDescription description, RootSignature rootSignature, InputLayoutDescription inputLayout)
    {
        description.CullMode ??= CullMode.None;
        description.RenderTargetCount ??= 1;
        description.RenderTargetFormat ??= GraphicsDevice.SwapChainFormat;
        description.DepthStencilFormat ??= GraphicsDevice.DepthStencilFormat;
        description.PrimitiveTopologyType ??= PrimitiveTopologyType.Triangle;

        foreach (var bundle in PipelineStateObjectBundles)
        {
            if (bundle.PipelineStateObjectDescription.Equals(description)
             && bundle.RootSignature.Equals(rootSignature)
             && bundle.InputLayout.Equals(inputLayout))
            {
                if (bundle.pipelineState is null)
                    throw new Exception("pipeline state error");

                return bundle.pipelineState;
            }
        }

        GraphicsPipelineStateDescription graphicsPipelineStateDescription = new()
        {
            RootSignature = rootSignature.Resource,
            VertexShader = VertexShader,
            GeometryShader = GeometryShader,
            PixelShader = PixelShader,
            PrimitiveTopologyType = description.PrimitiveTopologyType.Value,
            InputLayout = inputLayout,
            DepthStencilState = DepthStencilDescription.Default,
            DepthStencilFormat = description.DepthStencilFormat.Value,
            RenderTargetFormats = new Format[description.RenderTargetCount.Value],
        };
        Array.Fill(graphicsPipelineStateDescription.RenderTargetFormats, description.RenderTargetFormat.Value);

        graphicsPipelineStateDescription.BlendState = description.BlendState switch
        {
            "Alpha" => new(Blend.SourceAlpha, Blend.InverseSourceAlpha, Blend.One, Blend.InverseSourceAlpha),
            "Add" => BlendDescription.Additive,
            "Opaque" => BlendDescription.Opaque,
            _ => BlendDescription.AlphaBlend,
        };

        graphicsPipelineStateDescription.DepthStencilState = DepthStencilDescription.Default;

        graphicsPipelineStateDescription.SampleMask = uint.MaxValue;

        RasterizerDescription rasterizerState = new(description.CullMode.Value, description.Wireframe ? FillMode.Wireframe : FillMode.Solid)
        {
            DepthBias = description.DepthBias,
            SlopeScaledDepthBias = description.SlopeScaledDepthBias,
        };
        graphicsPipelineStateDescription.RasterizerState = rasterizerState;

        var pipelineState = device.Device.CreateGraphicsPipelineState<ID3D12PipelineState>(graphicsPipelineStateDescription);
        if (pipelineState is null)
            throw new Exception("pipeline state error");

        PipelineStateObjectBundles.Add(new()
        {
            PipelineStateObjectDescription = description,
            pipelineState = pipelineState,
            RootSignature = rootSignature,
            InputLayout = inputLayout
        });

        return pipelineState;
    }

    public void Dispose()
    {
        foreach (var combine in PipelineStateObjectBundles)
            combine.pipelineState.Dispose();

        PipelineStateObjectBundles.Clear();
    }
}

public sealed class PipelineStateObjectBundle
{
    public PipelineStateObjectDescription PipelineStateObjectDescription;
    public RootSignature RootSignature;
    public ID3D12PipelineState pipelineState;
    public InputLayoutDescription InputLayout;
}

public struct PipelineStateObjectDescription : IEquatable<PipelineStateObjectDescription>
{
    public int? RenderTargetCount;

    public Format? RenderTargetFormat;
    public Format? DepthStencilFormat;

    public string BlendState;
    public int DepthBias;
    public float SlopeScaledDepthBias;

    public CullMode? CullMode;

    public bool Wireframe;

    public string InputLayout;

    public PrimitiveTopologyType? PrimitiveTopologyType;

    public override bool Equals(object obj) =>
        obj is PipelineStateObjectDescription description && Equals(description);

    public bool Equals(PipelineStateObjectDescription other) =>
        RenderTargetCount == other.RenderTargetCount
     && RenderTargetFormat == other.RenderTargetFormat
     && DepthStencilFormat == other.DepthStencilFormat
     && BlendState == other.BlendState
     && DepthBias == other.DepthBias
     && SlopeScaledDepthBias == other.SlopeScaledDepthBias
     && CullMode == other.CullMode
     && InputLayout == other.InputLayout
     && PrimitiveTopologyType == other.PrimitiveTopologyType;

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(RenderTargetCount);
        hash.Add(RenderTargetFormat);
        hash.Add(DepthStencilFormat);
        hash.Add(BlendState);
        hash.Add(DepthBias);
        hash.Add(SlopeScaledDepthBias);
        hash.Add(CullMode);
        hash.Add(InputLayout);
        hash.Add(PrimitiveTopologyType);

        return hash.ToHashCode();
    }

    public static bool operator ==(PipelineStateObjectDescription x, PipelineStateObjectDescription y) =>
        x.Equals(y);

    public static bool operator !=(PipelineStateObjectDescription x, PipelineStateObjectDescription y) =>
        !(x == y);
}