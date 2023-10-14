using System.Drawing;
using System.Runtime.CompilerServices;

using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Data;

public struct RenderData
{
    public ID3D11DeviceContext DeviceContext;

    public SwapChainDescription1 SwapChainDescription;
    public IDXGISwapChain2 SwapChain;

    public ID3D11Texture2D BackBufferRenderTargetTexture;
    public ID3D11RenderTargetView BackBufferRenderTargetView;

    public Texture2DDescription MSAATextureDescription;
    public ID3D11Texture2D MSAARenderTargetTexture;
    public RenderTargetViewDescription MSAARenderTargetViewDescription;
    public ID3D11RenderTargetView MSAARenderTargetView;

    public RenderTargetBlendDescription RenderTargetBlendDescription;

    public BlendDescription BlendStateDescription;
    public ID3D11BlendState BlendState;

    public DepthStencilDescription DepthStencilDescription;
    public ID3D11DepthStencilState DepthStencilState;
    public Texture2DDescription DepthStencilTextureDescription;
    public ID3D11Texture2D DepthStencilTexture;
    public DepthStencilViewDescription DepthStencilViewDescription;
    public ID3D11DepthStencilView DepthStencilView;

    public RasterizerDescription RasterizerDescription;
    public ID3D11RasterizerState RasterizerState;

    public ID3D11Buffer VertexBuffer;
    public ID3D11Buffer IndexBuffer;

    public ID3D11VertexShader VertexShader;
    public ID3D11PixelShader PixelShader;

    public ID3D11InputLayout InputLayout;

    public PrimitiveTopology PrimitiveTopology;

    public Format Format;

    public void SetRasterizerDescFillModeWireframe() =>
        SetRasterizerDescFillMode(FillMode.Wireframe);

    public void SetRasterizerDescFillMode(FillMode fillMode = FillMode.Solid)
    {
        RasterizerDescription.FillMode = fillMode;
        RasterizerDescription.CullMode = fillMode == FillMode.Solid ? CullMode.Back : CullMode.None;
        RasterizerDescription.FrontCounterClockwise = true;
    }

    public void SetPrimitiveTopology(PrimitiveTopology primitiveTopology = PrimitiveTopology.TriangleList) =>
        PrimitiveTopology = primitiveTopology;

    public void SetConstantBufferVS(int slot, ID3D11Buffer constantBuffer) =>
        DeviceContext.VSSetConstantBuffer(slot, constantBuffer);

    public void SetConstantBufferPS(int slot, ID3D11Buffer constantBuffer) =>
        DeviceContext.PSSetConstantBuffer(slot, constantBuffer);

    public void SetResourceView(int slot, params ID3D11ShaderResourceView[] resourceViews) =>
        DeviceContext.PSSetShaderResources(slot, resourceViews);

    public void SetSamplerState(int slot, params ID3D11SamplerState[] samplerStates) =>
        DeviceContext.PSSetSamplers(slot, samplerStates);

    public void SetViewport(Size size) =>
        DeviceContext.RSSetViewport(new Viewport(0, 0, size.Width, size.Height, 0.0f, 1.0f));

    public void SetupRenderState(Format indexFormat = Format.R16_UInt, int? vertexStride = null, int vertexOffset = 0, int indexOffset = 0)
    {
        vertexStride ??= Unsafe.SizeOf<Vertex>();

        DeviceContext.IASetInputLayout(InputLayout);
        DeviceContext.IASetVertexBuffer(0, VertexBuffer, vertexStride.Value, vertexOffset);
        DeviceContext.IASetIndexBuffer(IndexBuffer, indexFormat, indexOffset);
        DeviceContext.IASetPrimitiveTopology(PrimitiveTopology);
        DeviceContext.VSSetShader(VertexShader);
        DeviceContext.PSSetShader(PixelShader);

        DeviceContext.OMSetBlendState(BlendState);
        DeviceContext.OMSetDepthStencilState(DepthStencilState);
        DeviceContext.RSSetState(RasterizerState);
    }

    public void SetupMaterial(
        ID3D11InputLayout inputLayout,
        ID3D11VertexShader vertexShader,
        ID3D11PixelShader pixelShader)
    {
        InputLayout = inputLayout;
        VertexShader = vertexShader;
        PixelShader = pixelShader;
    }

    public void Dispose()
    {
        DeviceContext?.Dispose();
        SwapChain?.Dispose();
        BackBufferRenderTargetTexture?.Dispose();
        BackBufferRenderTargetView?.Dispose();
        MSAARenderTargetTexture?.Dispose();
        MSAARenderTargetView?.Dispose();
        BlendState?.Dispose();
        DepthStencilTexture?.Dispose();
        DepthStencilView?.Dispose();
        VertexBuffer?.Dispose();
        IndexBuffer?.Dispose();
        VertexShader?.Dispose();
        PixelShader?.Dispose();
        InputLayout?.Dispose();
    }
}
