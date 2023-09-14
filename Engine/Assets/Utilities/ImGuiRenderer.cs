using ImGuiNET;
using System.Collections.Generic;
using System.IO;
using Windows.Foundation;

using Vortice.D3DCompiler;
using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice.Mathematics;

using ImDrawIdx = System.UInt16;
using Vortice;

// based on https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx11.cpp
// copied from https://github.com/YaakovDavis/VorticeImGui/blob/master/VorticeImGui/Framework/ImGuiRenderer.cs

namespace Engine.Editor;

unsafe public class ImGuiRenderer
{
    private const int _vertexConstantBufferSize = 16 * 4;

    private ID3D11Device _device;
    private ID3D11DeviceContext _deviceContext;
    private ID3D11Buffer _vertexBuffer;
    private ID3D11Buffer _indexBuffer;
    private Blob _vertexShaderBlob;
    private ID3D11VertexShader _vertexShader;
    private ID3D11InputLayout _inputLayout;
    private ID3D11Buffer _constantBuffer;
    private Blob _pixelShaderBlob;
    private ID3D11PixelShader _pixelShader;
    private ID3D11SamplerState _fontSampler;
    private ID3D11ShaderResourceView _fontTextureView;
    private ID3D11RasterizerState _rasterizerState;
    private ID3D11BlendState _blendState;
    private ID3D11DepthStencilState _depthStencilState;
    private int _vertexBufferSize = 5000, _indexBufferSize = 10000;

    private Dictionary<IntPtr, ID3D11ShaderResourceView> _textureResources = new();

    public ImGuiRenderer(ID3D11Device device, ID3D11DeviceContext deviceContext)
    {
        _device = device;
        _deviceContext = deviceContext;

        device.AddRef();
        deviceContext.AddRef();

        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;  // We can honor the ImDrawCmd::VtxOffset field, allowing for large meshes.

        CreateDeviceObjects();
    }

    public void Update(IntPtr imGuiContext)
    {
        ImGui.SetCurrentContext(imGuiContext);
        var io = ImGui.GetIO();

        io.DeltaTime = Time.DeltaF;

        ImGui.NewFrame();
        ImGui.Render();
    }

    public void Render(ImDrawDataPtr data)
    {
        // Avoid rendering when minimized
        if (data.DisplaySize.X <= 0.0f || data.DisplaySize.Y <= 0.0f)
            return;

        ID3D11DeviceContext ctx = _deviceContext;

        if (_vertexBuffer == null || _vertexBufferSize < data.TotalVtxCount)
        {
            _vertexBuffer?.Release();

            _vertexBufferSize = data.TotalVtxCount + 5000;
            BufferDescription desc = new BufferDescription();
            desc.Usage = ResourceUsage.Dynamic;
            desc.ByteWidth = _vertexBufferSize * sizeof(ImDrawVert);
            desc.BindFlags = BindFlags.VertexBuffer;
            desc.CPUAccessFlags = CpuAccessFlags.Write;
            _vertexBuffer = _device.CreateBuffer(desc);
        }

        if (_indexBuffer == null || _indexBufferSize < data.TotalIdxCount)
        {
            _indexBuffer?.Release();

            _indexBufferSize = data.TotalIdxCount + 10000;

            BufferDescription desc = new BufferDescription();
            desc.Usage = ResourceUsage.Dynamic;
            desc.ByteWidth = _indexBufferSize * sizeof(ImDrawIdx);
            desc.BindFlags = BindFlags.IndexBuffer;
            desc.CPUAccessFlags = CpuAccessFlags.Write;
            _indexBuffer = _device.CreateBuffer(desc);
        }

        // Upload vertex/index data into a single contiguous GPU buffer
        var vertexResource = ctx.Map(_vertexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        var indexResource = ctx.Map(_indexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        var vertexResourcePointer = (ImDrawVert*)vertexResource.DataPointer;
        var indexResourcePointer = (ImDrawIdx*)indexResource.DataPointer;
        for (int n = 0; n < data.CmdListsCount; n++)
        {
            var cmdlList = data.CmdListsRange[n];

            var vertBytes = cmdlList.VtxBuffer.Size * sizeof(ImDrawVert);
            Buffer.MemoryCopy((void*)cmdlList.VtxBuffer.Data, vertexResourcePointer, vertBytes, vertBytes);

            var idxBytes = cmdlList.IdxBuffer.Size * sizeof(ImDrawIdx);
            Buffer.MemoryCopy((void*)cmdlList.IdxBuffer.Data, indexResourcePointer, idxBytes, idxBytes);

            vertexResourcePointer += cmdlList.VtxBuffer.Size;
            indexResourcePointer += cmdlList.IdxBuffer.Size;
        }
        ctx.Unmap(_vertexBuffer, 0);
        ctx.Unmap(_indexBuffer, 0);

        // Setup orthographic projection matrix into our constant buffer
        // Our visible imGui space lies from draw_data.DisplayPos (top left) to draw_data.DisplayPos+data_data.DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.

        var constResource = ctx.Map(_constantBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        var span = constResource.AsSpan<float>(_vertexConstantBufferSize);
        float L = data.DisplayPos.X;
        float R = data.DisplayPos.X + data.DisplaySize.X;
        float T = data.DisplayPos.Y;
        float B = data.DisplayPos.Y + data.DisplaySize.Y;
        float[] mvp =
        {
                2.0f/(R-L),   0.0f,           0.0f,       0.0f,
                0.0f,         2.0f/(T-B),     0.0f,       0.0f,
                0.0f,         0.0f,           0.5f,       0.0f,
                (R+L)/(L-R),  (T+B)/(B-T),    0.5f,       1.0f,
        };
        mvp.CopyTo(span);
        ctx.Unmap(_constantBuffer, 0);

        SetupRenderState(data, ctx);

        // Render command lists
        // (Because we merged all buffers into a single one, we maintain our own offset into them)
        int global_idx_offset = 0;
        int global_vtx_offset = 0;
        Vector2 clip_off = data.DisplayPos;
        for (int n = 0; n < data.CmdListsCount; n++)
        {
            var cmdList = data.CmdListsRange[n];
            for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
            {
                var cmd = cmdList.CmdBuffer[i];
                if (cmd.UserCallback != IntPtr.Zero)
                    throw new NotImplementedException("user callbacks not implemented");
                else
                {
                    var rect = new RawRect((int)(cmd.ClipRect.X - clip_off.X), (int)(cmd.ClipRect.Y - clip_off.Y), (int)(cmd.ClipRect.Z - clip_off.X), (int)(cmd.ClipRect.W - clip_off.Y));
                    ctx.RSSetScissorRects(new[] { rect });

                    _textureResources.TryGetValue(cmd.TextureId, out var texture);
                    if (texture != null)
                        ctx.PSSetShaderResources(0, new[] { texture });

                    ctx.DrawIndexed((int)cmd.ElemCount, (int)(cmd.IdxOffset + global_idx_offset), (int)(cmd.VtxOffset + global_vtx_offset));
                }
            }
            global_idx_offset += cmdList.IdxBuffer.Size;
            global_vtx_offset += cmdList.VtxBuffer.Size;
        }
    }

    public void Dispose()
    {
        if (_device is null)
            return;

        InvalidateDeviceObjects();

        ReleaseAndNullify(ref _device);
        ReleaseAndNullify(ref _deviceContext);
    }

    private void SetupRenderState(ImDrawDataPtr drawData, ID3D11DeviceContext ctx)
    {
        var viewport = new Viewport
        {
            Width = drawData.DisplaySize.X,
            Height = drawData.DisplaySize.Y,
            MinDepth = 0.0f,
            MaxDepth = 1.0f,
        };
        ctx.RSSetViewports(new[] { viewport });

        int stride = sizeof(ImDrawVert);
        int offset = 0;

        ctx.IASetInputLayout(_inputLayout);
        ctx.IASetVertexBuffers(0, 1, new[] { _vertexBuffer }, new[] { stride }, new[] { offset });
        ctx.IASetIndexBuffer(_indexBuffer, sizeof(ImDrawIdx) == 2 ? Format.R16_UInt : Format.R32_UInt, 0);
        ctx.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        ctx.VSSetShader(_vertexShader);
        ctx.VSSetConstantBuffers(0, new[] { _constantBuffer });
        ctx.PSSetShader(_pixelShader);
        ctx.PSSetSamplers(0, new[] { _fontSampler });
        ctx.GSSetShader(null);
        ctx.HSSetShader(null);
        ctx.DSSetShader(null);
        ctx.CSSetShader(null);

        ctx.OMSetBlendState(_blendState);
        ctx.OMSetDepthStencilState(_depthStencilState);
        ctx.RSSetState(_rasterizerState);
    }

    private void CreateFontsTexture()
    {
        var io = ImGui.GetIO();
        byte* pixels;
        int width, height;
        io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height);

        Texture2DDescription texDesc = new()
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription { Count = 1 },
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.None
        };

        SubresourceData subResource = new()
        {
            DataPointer = (IntPtr)pixels,
            RowPitch = texDesc.Width * 4,
            SlicePitch = 0
        };

        var texture = _device.CreateTexture2D(texDesc, new[] { subResource });

        ShaderResourceViewDescription resViewDesc = new()
        {
            Format = Format.R8G8B8A8_UNorm,
            ViewDimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new Texture2DShaderResourceView { MipLevels = texDesc.MipLevels, MostDetailedMip = 0 }
        };
        _fontTextureView = _device.CreateShaderResourceView(texture, resViewDesc);
        texture.Release();

        io.Fonts.TexID = RegisterTexture(_fontTextureView);

        SamplerDescription samplerDesc = new()
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            MipLODBias = 0f,
            ComparisonFunc = ComparisonFunction.Always,
            MinLOD = 0f,
            MaxLOD = 0f
        };
        _fontSampler = _device.CreateSamplerState(samplerDesc);
    }

    private IntPtr RegisterTexture(ID3D11ShaderResourceView texture)
    {
        var imGuiID = texture.NativePointer;
        _textureResources.Add(imGuiID, texture);

        return imGuiID;
    }

    private void CreateDeviceObjects()
    {
        ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode("ImGui.hlsl", "VS", "vs_4_0");

        _vertexShader = _device.CreateVertexShader(vertexShaderByteCode.Span);

        var inputElements = new[]
        {
                new InputElementDescription( "POSITION", 0, Format.R32G32_Float,   0, 0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "TEXCOORD", 0, Format.R32G32_Float,   8,  0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "COLOR",    0, Format.R8G8B8A8_UNorm, 16, 0, InputClassification.PerVertexData, 0 ),
            };

        _inputLayout = _device.CreateInputLayout(inputElements, vertexShaderByteCode.Span);

        BufferDescription constBufferDesc = new()
        {
            ByteWidth = _vertexConstantBufferSize,
            Usage = ResourceUsage.Dynamic,
            BindFlags = BindFlags.ConstantBuffer,
            CPUAccessFlags = CpuAccessFlags.Write
        };
        _constantBuffer = _device.CreateBuffer(constBufferDesc);

        ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode("ImGui.hlsl", "PS", "ps_4_0");

        _pixelShader = _device.CreatePixelShader(pixelShaderByteCode.Span);

        RenderTargetBlendDescription renTarDesc = new()
        {
            BlendEnable = true, // Enable blend.
            SourceBlend = Blend.SourceAlpha,
            DestinationBlend = Blend.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceBlendAlpha = Blend.One,
            DestinationBlendAlpha = Blend.Zero,
            BlendOperationAlpha = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteEnable.All
        };

        BlendDescription blendDesc = new()
        {
            AlphaToCoverageEnable = false
        };

        blendDesc.RenderTarget[0] = new()
        {
            BlendEnable = true,
            SourceBlend = Blend.SourceAlpha,
            DestinationBlend = Blend.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceBlendAlpha = Blend.InverseSourceAlpha,
            DestinationBlendAlpha = Blend.Zero,
            BlendOperationAlpha = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteEnable.All
        };

        blendDesc.RenderTarget[0] = renTarDesc;

        _blendState = _device.CreateBlendState(blendDesc);

        RasterizerDescription rasterDesc = new()
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.None,
            ScissorEnable = true,
            DepthClipEnable = true
        };

        _rasterizerState = _device.CreateRasterizerState(rasterDesc);

        DepthStencilOperationDescription stencilOpDesc = new(StencilOperation.Keep, StencilOperation.Keep, StencilOperation.Keep, ComparisonFunction.Always);
        DepthStencilDescription depthDesc = new()
        {
            DepthEnable = false,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc = ComparisonFunction.Always,
            StencilEnable = false,
            FrontFace = stencilOpDesc,
            BackFace = stencilOpDesc
        };

        _depthStencilState = _device.CreateDepthStencilState(depthDesc);

        CreateFontsTexture();
    }

    private void InvalidateDeviceObjects()
    {
        ReleaseAndNullify(ref _fontSampler);
        ReleaseAndNullify(ref _fontTextureView);
        ReleaseAndNullify(ref _indexBuffer);
        ReleaseAndNullify(ref _vertexBuffer);
        ReleaseAndNullify(ref _blendState);
        ReleaseAndNullify(ref _depthStencilState);
        ReleaseAndNullify(ref _rasterizerState);
        ReleaseAndNullify(ref _pixelShader);
        ReleaseAndNullify(ref _pixelShaderBlob);
        ReleaseAndNullify(ref _constantBuffer);
        ReleaseAndNullify(ref _inputLayout);
        ReleaseAndNullify(ref _vertexShader);
        ReleaseAndNullify(ref _vertexShaderBlob);
    }

    private void ReleaseAndNullify<T>(ref T o) where T : SharpGen.Runtime.ComObject
    {
        o.Release();
        o = null;
    }

    protected static ReadOnlyMemory<byte> CompileBytecode(string shaderName, string entryPoint, string profile)
    {
        string assetsPath = Path.Combine(AppContext.BaseDirectory, Paths.SHADERS);
        string fileName = Path.Combine(assetsPath, shaderName);

        ShaderFlags shaderFlags = ShaderFlags.EnableStrictness;
#if DEBUG
        shaderFlags |= ShaderFlags.Debug;
        shaderFlags |= ShaderFlags.SkipValidation;
#else
        shaderFlags |= ShaderFlags.OptimizationLevel3;
#endif

        return Compiler.CompileFromFile(fileName, entryPoint, profile, shaderFlags);
    }
}
