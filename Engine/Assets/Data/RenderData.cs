using System.Collections.Generic;

using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.DXGI;

namespace Engine.Data;

public struct RenderData
{
    public IDXGISwapChain2 SwapChain;

    public ID3D11Texture2D RenderTargetTexture;
    public ID3D11RenderTargetView RenderTargetView;

    public ID3D11RasterizerState RasterizerState;
    public RasterizerDescription RasterizerDescription;

    public ID3D11BlendState BlendState;
    public BlendDescription BlendStateDescription;

    public ID3D11DepthStencilState DepthStencilState;
    public Texture2DDescription DepthStencilTextureDescription;
    public ID3D11Texture2D DepthStencilTexture;
    public ID3D11DepthStencilView DepthStencilView;

    public ID3D11Buffer VertexBuffer;
    public ID3D11Buffer IndexBuffer;

    public ID3D11VertexShader VertexShader;
    public ID3D11PixelShader PixelShader;

    public ID3D11InputLayout InputLayout;

    public ID3D11Buffer ConstantBuffer;

    public ID3D11SamplerState FontSampler;
    public ID3D11ShaderResourceView FontTextureView;

    public Dictionary<IntPtr, ID3D11ShaderResourceView> TextureResources;

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

    public IntPtr RegisterTexture(ID3D11ShaderResourceView texture)
    {
        var id = texture.NativePointer;
        TextureResources.Add(id, texture);

        return id;
    }

    public void SetPrimitiveTopology(PrimitiveTopology primitiveTopology) 
        => PrimitiveTopology = primitiveTopology;
}
