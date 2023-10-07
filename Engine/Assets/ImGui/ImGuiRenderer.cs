using System.Collections.Generic;
using System.Drawing;

using ImGuiNET;
using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice;

using ImDrawIdx = System.UInt16;

// based on https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx11.cpp
// copied from https://github.com/YaakovDavis/VorticeImGui/blob/master/VorticeImGui/Framework/ImGuiRenderer.cs

namespace Engine.Gui;

unsafe public sealed class ImGuiRenderer
{
    public bool IsRendering { get => _data.RenderTargetView is not null; }

    private ID3D11Device _device;
    private RenderData _data = new();

    private int _vertexBufferSize = 5000, _indexBufferSize = 10000;
    private const int _vertexConstantBufferSize = 16 * 4;

    private ID3D11Buffer _viewConstantBuffer;

    private ID3D11SamplerState _samplerState;
    private ID3D11ShaderResourceView _resourceView;
    private Dictionary<IntPtr, ID3D11ShaderResourceView> _textureResources = new();

    public ImGuiRenderer()
    {
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;  // We can honor the ImDrawCmd::VtxOffset field, allowing for large meshes.

        _data = Core.Instance.Renderer.Data; // Make a copy of the Renderers Data and fill it with CreateMaterial
        _device = Core.Instance.Renderer.Device;

        CreateMaterial();
    }

    public void Update(IntPtr imGuiContext, Size newSize)
    {
        ImGui.SetCurrentContext(imGuiContext);
        var io = ImGui.GetIO();

        io.DeltaTime = Time.DeltaF;
        io.DisplaySize = newSize.ToVector2();

        ImGui.NewFrame();
    }

    public void Render() =>
        Draw(ImGui.GetDrawData());

    private void Draw(ImDrawDataPtr data)
    {
        // Avoid rendering when minimized
        if (data.DisplaySize.X <= 0.0f || data.DisplaySize.Y <= 0.0f)
            return;

        #region // Create Vertex and Index Buffer
        if (_data.VertexBuffer == null || _vertexBufferSize < data.TotalVtxCount)
        {
            _data.VertexBuffer?.Release();

            _vertexBufferSize = data.TotalVtxCount + 5000;
            BufferDescription desc = new BufferDescription();
            desc.Usage = ResourceUsage.Dynamic;
            desc.ByteWidth = _vertexBufferSize * sizeof(ImDrawVert);
            desc.BindFlags = BindFlags.VertexBuffer;
            desc.CPUAccessFlags = CpuAccessFlags.Write;
            _data.VertexBuffer = _device.CreateBuffer(desc);
        }

        if (_data.IndexBuffer == null || _indexBufferSize < data.TotalIdxCount)
        {
            _data.IndexBuffer?.Release();

            _indexBufferSize = data.TotalIdxCount + 10000;

            BufferDescription desc = new BufferDescription();
            desc.Usage = ResourceUsage.Dynamic;
            desc.ByteWidth = _indexBufferSize * sizeof(ImDrawIdx);
            desc.BindFlags = BindFlags.IndexBuffer;
            desc.CPUAccessFlags = CpuAccessFlags.Write;
            _data.IndexBuffer = _device.CreateBuffer(desc);
        }
        #endregion

        #region // Set Buffers
        // Upload vertex/index data into a single contiguous GPU buffer
        var vertexResource = _data.DeviceContext.Map(_data.VertexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        var indexResource = _data.DeviceContext.Map(_data.IndexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        var vertexResourcePointer = (ImDrawVert*)vertexResource.DataPointer;
        var indexResourcePointer = (ImDrawIdx*)indexResource.DataPointer;
        for (int n = 0; n < data.CmdListsCount; n++)
        {
            var cmdlList = data.CmdLists[n];

            var vertBytes = cmdlList.VtxBuffer.Size * sizeof(ImDrawVert);
            Buffer.MemoryCopy((void*)cmdlList.VtxBuffer.Data, vertexResourcePointer, vertBytes, vertBytes);

            var idxBytes = cmdlList.IdxBuffer.Size * sizeof(ImDrawIdx);
            Buffer.MemoryCopy((void*)cmdlList.IdxBuffer.Data, indexResourcePointer, idxBytes, idxBytes);

            vertexResourcePointer += cmdlList.VtxBuffer.Size;
            indexResourcePointer += cmdlList.IdxBuffer.Size;
        }
        _data.DeviceContext.Unmap(_data.VertexBuffer, 0);
        _data.DeviceContext.Unmap(_data.IndexBuffer, 0);
        #endregion

        #region // Set Model View Projection Matrix
        // Setup orthographic projection matrix into our constant buffer
        // Our visible imGui space lies from draw_data.DisplayPos (top left) to draw_data.DisplayPos+data_data.DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.
        var constResource = _data.DeviceContext.Map(_viewConstantBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
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
        _data.DeviceContext.Unmap(_viewConstantBuffer, 0);

        _data.SetConstantBuffer(0, _viewConstantBuffer);
        #endregion

        #region // Setup Render State
        _data.PrimitiveTopology = PrimitiveTopology.TriangleList;
        _data.SetupRenderState(
            sizeof(ImDrawIdx) == 2 ? Format.R16_UInt : Format.R32_UInt, 
            sizeof(ImDrawVert));
        _data.SetSamplerState(0, _samplerState);
        _data.SetResourceView(0, _resourceView);
        #endregion

        #region // Render command lists
        // (Because we merged all buffers into a single one, we maintain our own offset into them)
        int global_idx_offset = 0;
        int global_vtx_offset = 0;
        Vector2 clip_off = data.DisplayPos;
        for (int n = 0; n < data.CmdListsCount; n++)
        {
            var cmdList = data.CmdLists[n];
            for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
            {
                var cmd = cmdList.CmdBuffer[i];
                if (cmd.UserCallback != IntPtr.Zero)
                    throw new NotImplementedException("user callbacks not implemented");
                else
                {
                    var rect = new RawRect((int)(cmd.ClipRect.X - clip_off.X), (int)(cmd.ClipRect.Y - clip_off.Y), (int)(cmd.ClipRect.Z - clip_off.X), (int)(cmd.ClipRect.W - clip_off.Y));
                    _data.DeviceContext.RSSetScissorRects(new[] { rect });

                    _textureResources.TryGetValue(cmd.TextureId, out var texture);
                    if (texture != null)
                        _data.DeviceContext.PSSetShaderResources(0, new[] { texture });

                    _data.DeviceContext.DrawIndexed((int)cmd.ElemCount, (int)(cmd.IdxOffset + global_idx_offset), (int)(cmd.VtxOffset + global_vtx_offset));
                }
            }
            global_idx_offset += cmdList.IdxBuffer.Size;
            global_vtx_offset += cmdList.VtxBuffer.Size;
        }
        #endregion
    }

    private void CreateMaterial()
    {
        #region // Create Vertex and Pixel Shader
        ReadOnlyMemory<byte> vertexShaderByteCode = Material.CompileBytecode("ImGui.hlsl", "VS", "vs_4_0");
        _data.VertexShader = _device.CreateVertexShader(vertexShaderByteCode.Span);

        ReadOnlyMemory<byte> pixelShaderByteCode = Material.CompileBytecode("ImGui.hlsl", "PS", "ps_4_0");
        _data.PixelShader = _device.CreatePixelShader(pixelShaderByteCode.Span);
        #endregion

        #region // Create Input Layout
        var inputElements = new[]
        {
                new InputElementDescription( "POSITION", 0, Format.R32G32_Float,   0, 0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "TEXCOORD", 0, Format.R32G32_Float,   8,  0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "COLOR",    0, Format.R8G8B8A8_UNorm, 16, 0, InputClassification.PerVertexData, 0 ),
        };
        _data.InputLayout = _device.CreateInputLayout(inputElements, vertexShaderByteCode.Span);
        #endregion

        #region // Create View Constant Buffer
        BufferDescription constBufferDesc = new()
        {
            ByteWidth = _vertexConstantBufferSize,
            Usage = ResourceUsage.Dynamic,
            BindFlags = BindFlags.ConstantBuffer,
            CPUAccessFlags = CpuAccessFlags.Write
        };
        _viewConstantBuffer = _device.CreateBuffer(constBufferDesc);
        #endregion

        #region // Create Blend, Rasterizer and Stencil Description
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
            SourceBlendAlpha = Blend.One,
            DestinationBlendAlpha = Blend.Zero,
            BlendOperationAlpha = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteEnable.All
        };
        _data.BlendState = _device.CreateBlendState(blendDesc);

        RasterizerDescription rasterDesc = new()
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.None,
            ScissorEnable = true,
            DepthClipEnable = true
        };

        _data.RasterizerState = _device.CreateRasterizerState(rasterDesc);

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

        _data.DepthStencilState = _device.CreateDepthStencilState(depthDesc);
        #endregion

        CreateFontsTexture();
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
        _resourceView = _device.CreateShaderResourceView(texture, resViewDesc);
        texture.Release();

        var id = _resourceView.NativePointer;
        _textureResources.Add(id, _resourceView);

        io.Fonts.TexID = id;

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
        _samplerState = _device.CreateSamplerState(samplerDesc);
    }
}
