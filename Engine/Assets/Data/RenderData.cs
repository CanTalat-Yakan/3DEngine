using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice.Mathematics;
using System.Drawing;

namespace Engine.Data;

public struct RenderData
{
    public ID3D11DeviceContext DeviceContext;

    public SwapChainDescription1 SwapChainDescription;
    public IDXGISwapChain2 SwapChain;

    public ID3D11Texture2D RenderTargetTexture;
    public ID3D11RenderTargetView RenderTargetView;

    public RenderTargetBlendDescription RenderTargetBlendDescription;

    public BlendDescription BlendStateDescription;
    public ID3D11BlendState BlendState;

    public DepthStencilDescription DepthStencilDescription;
    public ID3D11DepthStencilState DepthStencilState;

    public Texture2DDescription DepthStencilTextureDescription;
    public ID3D11Texture2D DepthStencilTexture;
    public ID3D11DepthStencilView DepthStencilView;

    public RasterizerDescription RasterizerDescription;
    public ID3D11RasterizerState RasterizerState;

    public ID3D11Buffer VertexBuffer;
    public ID3D11Buffer IndexBuffer;

    public ID3D11VertexShader VertexShader;
    public ID3D11PixelShader PixelShader;

    public ID3D11InputLayout InputLayout;

    public ID3D11SamplerState SamplerState;
    public ID3D11ShaderResourceView ResourceView;

    public PrimitiveTopology PrimitiveTopology;

    public bool VSync;
    public bool SuperSample;

    public void SetVsync(bool b) => 
        VSync = b;

    public void SetSuperSample(bool b) => 
        SuperSample = b;

    public void SetRasterizerDescFillModeWireframe() => 
        SetRasterizerDescFillMode(FillMode.Wireframe); 

    public void SetRasterizerDescFillMode(FillMode fillmode = FillMode.Solid) 
    {
        RasterizerDescription.FillMode = fillmode;
        RasterizerDescription.CullMode = fillmode == FillMode.Solid ? CullMode.Back : CullMode.None;
        RasterizerDescription.FrontCounterClockwise = true;
    }

    public void SetPrimitiveTopology(PrimitiveTopology primitiveTopology) => 
        PrimitiveTopology = primitiveTopology;

    public void SetConstantBuffer(int slot, ID3D11Buffer constantBuffer) =>
        DeviceContext.VSSetConstantBuffer(slot, constantBuffer);

    public void SetViewport(Size size) =>
        DeviceContext.RSSetViewport(new Viewport(0, 0, size.Width, size.Height, 0.0f, 1.0f));

    public void SetupRenderState(int vertexStride, int vertexOffset, Format indexFormat = Format.R16_UInt, int indexOffset = 0)
    {
        DeviceContext.IASetInputLayout(InputLayout);
        DeviceContext.IASetVertexBuffer(0, VertexBuffer, vertexStride, vertexOffset);
        DeviceContext.IASetIndexBuffer(IndexBuffer, indexFormat, indexOffset);
        DeviceContext.IASetPrimitiveTopology(PrimitiveTopology);
        DeviceContext.VSSetShader(VertexShader);
        DeviceContext.PSSetShader(PixelShader);
        DeviceContext.PSSetSampler(0, SamplerState);
        DeviceContext.PSSetShaderResource(0, ResourceView);

        DeviceContext.OMSetBlendState(BlendState);
        DeviceContext.OMSetDepthStencilState(DepthStencilState);
        DeviceContext.RSSetState(RasterizerState);
    }

    public void SetupMaterial(
        ID3D11InputLayout inputLayout,
        ID3D11VertexShader vertexShader,
        ID3D11PixelShader pixelShader,
        ID3D11SamplerState sampler,
        ID3D11ShaderResourceView resourceView)
    {
        InputLayout = inputLayout;
        VertexShader = vertexShader;
        PixelShader = pixelShader;
        SamplerState = sampler;
        ResourceView = resourceView;
    }
}
