using ImGuiNET;
using System.Collections.Generic;
using System.IO;
using Vortice.D3DCompiler;
using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.DXGI;
using Windows.Foundation;
using ImDrawIdx = System.UInt16;

// based on https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx11.cpp
// copied from https://github.com/YaakovDavis/VorticeImGui/blob/master/VorticeImGui/Framework/ImGuiRenderer.cs

namespace Engine.Editor;

unsafe public class ImGuiRenderer
{
    const int VertexConstantBufferSize = 16 * 4;

    ID3D11Device device;
    ID3D11DeviceContext deviceContext;
    ID3D11Buffer vertexBuffer;
    ID3D11Buffer indexBuffer;
    Blob vertexShaderBlob;
    ID3D11VertexShader vertexShader;
    ID3D11InputLayout inputLayout;
    ID3D11Buffer constantBuffer;
    Blob pixelShaderBlob;
    ID3D11PixelShader pixelShader;
    ID3D11SamplerState fontSampler;
    ID3D11ShaderResourceView fontTextureView;
    ID3D11RasterizerState rasterizerState;
    ID3D11BlendState blendState;
    ID3D11DepthStencilState depthStencilState;
    int vertexBufferSize = 5000, indexBufferSize = 10000;

    Dictionary<IntPtr, ID3D11ShaderResourceView> textureResources = new();

    private static readonly string SHADER_IMGUI = @"Resources\Shader\ImGui.hlsl";

    public ImGuiRenderer()
    {
        this.device = Renderer.Instance.Device;
        this.deviceContext = Renderer.Instance.DeviceContext;

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

        io.DeltaTime = (float)Time.Delta;

        ImGui.NewFrame();
        ImGui.Render();
    }

    public void Render(ImDrawDataPtr data)
    {
        // Avoid rendering when minimized
        if (data.DisplaySize.X <= 0.0f || data.DisplaySize.Y <= 0.0f)
            return;

        ID3D11DeviceContext ctx = deviceContext;

        if (vertexBuffer == null || vertexBufferSize < data.TotalVtxCount)
        {
            vertexBuffer?.Release();

            vertexBufferSize = data.TotalVtxCount + 5000;
            BufferDescription desc = new()
            {
                Usage = ResourceUsage.Dynamic,
                ByteWidth = vertexBufferSize * sizeof(ImDrawVert),
                BindFlags = BindFlags.ConstantBuffer,
                CPUAccessFlags = CpuAccessFlags.Write,
            };
            vertexBuffer = device.CreateBuffer(desc);
        }

        if (indexBuffer == null || indexBufferSize < data.TotalIdxCount)
        {
            indexBuffer?.Release();

            indexBufferSize = data.TotalIdxCount + 10000;
            BufferDescription desc = new()
            {
                Usage = ResourceUsage.Dynamic,
                ByteWidth = indexBufferSize * sizeof(ImDrawIdx),
                BindFlags = BindFlags.IndexBuffer,
                CPUAccessFlags = CpuAccessFlags.Write,
            };
            indexBuffer = device.CreateBuffer(desc);
        }

        // Upload vertex/index data into a single contiguous GPU buffer
        var vertexResource = ctx.Map(vertexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        var indexResource = ctx.Map(indexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
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
        ctx.Unmap(vertexBuffer, 0);
        ctx.Unmap(indexBuffer, 0);

        // Setup orthographic projection matrix into our constant buffer
        // Our visible imgui space lies from draw_data.DisplayPos (top left) to draw_data.DisplayPos+data_data.DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.

        var constResource = ctx.Map(constantBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        var span = constResource.AsSpan<float>(VertexConstantBufferSize);
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
        ctx.Unmap(constantBuffer, 0);

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
                    var rect = new Rect((int)(cmd.ClipRect.X - clip_off.X), (int)(cmd.ClipRect.Y - clip_off.Y), (int)(cmd.ClipRect.Z - clip_off.X), (int)(cmd.ClipRect.W - clip_off.Y));
                    ctx.RSSetScissorRect(rect);

                    textureResources.TryGetValue(cmd.TextureId, out var texture);
                    if (texture is not null)
                        ctx.PSSetShaderResource(0, texture);

                    ctx.DrawIndexed((int)cmd.ElemCount, (int)(cmd.IdxOffset + global_idx_offset), (int)(cmd.VtxOffset + global_vtx_offset));
                }
            }
            global_idx_offset += cmdList.IdxBuffer.Size;
            global_vtx_offset += cmdList.VtxBuffer.Size;
        }
    }

    public void Dispose()
    {
        if (device == null)
            return;

        InvalidateDeviceObjects();

        ReleaseAndNullify(ref device);
        ReleaseAndNullify(ref deviceContext);
    }

    void ReleaseAndNullify<T>(ref T o) where T : SharpGen.Runtime.ComObject
    {
        o.Release();
        o = null;
    }

    void SetupRenderState(ImDrawDataPtr drawData, ID3D11DeviceContext ctx)
    {
        int stride = sizeof(ImDrawVert);
        int offset = 0;

        ctx.IASetInputLayout(inputLayout);
        //ctx.IASetVertexBuffers(0, 1, new[] { vertexBuffer }, new[] { stride }, new[] { offset });
        //ctx.IASetVertexBuffer(0, vertexBuffer, stride, offset);
        ctx.IASetIndexBuffer(indexBuffer, sizeof(ImDrawIdx) == 2 ? Format.R16_UInt : Format.R32_UInt, 0);
        ctx.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        ctx.VSSetShader(vertexShader);
        ctx.VSSetConstantBuffer(0, constantBuffer);
        ctx.PSSetShader(pixelShader);
        ctx.PSSetSampler(0, fontSampler);
        ctx.GSSetShader(null);
        ctx.HSSetShader(null);
        ctx.DSSetShader(null);
        ctx.CSSetShader(null);

        //ctx.OMSetBlendState(blendState);
        //ctx.OMSetDepthStencilState(depthStencilState);
        //ctx.RSSetState(rasterizerState);
    }

    void CreateFontsTexture()
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

        var texture = device.CreateTexture2D(texDesc, new[] { subResource });

        ShaderResourceViewDescription resViewDesc = new()
        {
            Format = Format.R8G8B8A8_UNorm,
            ViewDimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new Texture2DShaderResourceView { MipLevels = texDesc.MipLevels, MostDetailedMip = 0 }
        };
        fontTextureView = device.CreateShaderResourceView(texture, resViewDesc);
        texture.Release();

        io.Fonts.TexID = RegisterTexture(fontTextureView);

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
        fontSampler = device.CreateSamplerState(samplerDesc);
    }

    IntPtr RegisterTexture(ID3D11ShaderResourceView texture)
    {
        var imguiID = texture.NativePointer;
        textureResources.Add(imguiID, texture);

        return imguiID;
    }

    void CreateDeviceObjects()
    {
        //Compiler.Compile(vertexShaderCode, "main", "vs", "vs_4_0", out vertexShaderBlob, out var errorBlob);
        //if (vertexShaderBlob == null)
        //throw new Exception("error compiling vertex shader");
        ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode(SHADER_IMGUI, "VS", "vs_4_0");

        vertexShader = device.CreateVertexShader(vertexShaderByteCode.Span);

        var inputElements = new[]
        {
                new InputElementDescription( "POSITION", 0, Format.R32G32_Float,   0, 0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "TEXCOORD", 0, Format.R32G32_Float,   8,  0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "COLOR",    0, Format.R8G8B8A8_UNorm, 16, 0, InputClassification.PerVertexData, 0 ),
            };

        inputLayout = device.CreateInputLayout(inputElements, vertexShaderByteCode.Span);

        BufferDescription constBufferDesc = new()
        {
            ByteWidth = VertexConstantBufferSize,
            Usage = ResourceUsage.Dynamic,
            BindFlags = BindFlags.ConstantBuffer,
            CPUAccessFlags = CpuAccessFlags.Write
        };
        constantBuffer = device.CreateBuffer(constBufferDesc);

        //Compiler.Compile(pixelShaderCode, "main", "ps", "ps_4_0", out pixelShaderBlob, out errorBlob);
        //if (pixelShaderBlob == null)
        //throw new Exception("error compiling pixel shader");
        ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode(SHADER_IMGUI, "PS", "ps_4_0");

        pixelShader = device.CreatePixelShader(pixelShaderByteCode.Span);

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

        blendState = device.CreateBlendState(blendDesc);

        RasterizerDescription rasterDesc = new()
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.None,
            ScissorEnable = true,
            DepthClipEnable = true
        };

        rasterizerState = device.CreateRasterizerState(rasterDesc);

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

        depthStencilState = device.CreateDepthStencilState(depthDesc);

        CreateFontsTexture();
    }

    void InvalidateDeviceObjects()
    {
        ReleaseAndNullify(ref fontSampler);
        ReleaseAndNullify(ref fontTextureView);
        ReleaseAndNullify(ref indexBuffer);
        ReleaseAndNullify(ref vertexBuffer);
        ReleaseAndNullify(ref blendState);
        ReleaseAndNullify(ref depthStencilState);
        ReleaseAndNullify(ref rasterizerState);
        ReleaseAndNullify(ref pixelShader);
        ReleaseAndNullify(ref pixelShaderBlob);
        ReleaseAndNullify(ref constantBuffer);
        ReleaseAndNullify(ref inputLayout);
        ReleaseAndNullify(ref vertexShader);
        ReleaseAndNullify(ref vertexShaderBlob);
    }

    protected static ReadOnlyMemory<byte> CompileBytecode(string shaderName, string entryPoint, string profile)
    {
        string assetsPath = Path.Combine(AppContext.BaseDirectory, @"Assets\Resources\");
        string fileName = Path.Combine(assetsPath, shaderName);
        //string shaderSource = File.ReadAllText(Path.Combine(assetsPath, shaderName));

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
