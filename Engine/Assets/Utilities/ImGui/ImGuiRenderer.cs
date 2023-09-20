using ImGuiNET;
using System.IO;

using Vortice.D3DCompiler;
using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice;

using ImDrawIdx = System.UInt16;
using System.Collections.Generic;

// based on https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx11.cpp
// copied from https://github.com/YaakovDavis/VorticeImGui/blob/master/VorticeImGui/Framework/ImGuiRenderer.cs

namespace Engine.Utilities;

unsafe public sealed class ImGuiRenderer
{
    public bool IsRendering { get => _data.RenderTargetView is not null; }

    private RenderData _data = new();

    private ID3D11Device _device;
    private ID3D11DeviceContext _deviceContext;

    private int _vertexBufferSize = 5000, _indexBufferSize = 10000;
    private const int _vertexConstantBufferSize = 16 * 4;

    private Dictionary<IntPtr, ID3D11ShaderResourceView> _textureResources = new();

    private Win32Window _win32Window;

    public ImGuiRenderer(Win32Window win32Window)
    {
        _win32Window = win32Window;

        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;  // We can honor the ImDrawCmd::VtxOffset field, allowing for large meshes.

        D3D11.D3D11CreateDevice(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            null,
            out _device,
            out _deviceContext);

        _data.DeviceContext = _deviceContext;

        InitializeSwapChain();
        CreateMaterial();
    }

    public void InitializeSwapChain()
    {
        //var dxgiFactory = _device.QueryInterface<IDXGIDevice>().GetAdapter().GetParent<IDXGIFactory>();
        //dxgiFactory.CreateSwapChain(_device, new());
        //dxgiFactory.MakeWindowAssociation(_win32Window.Handle, WindowAssociationFlags.Valid);

        // Initialize the SwapChainDescription structure.
        SwapChainDescription1 swapChainDescription1 = new()
        {
            AlphaMode = AlphaMode.Ignore,
            BufferCount = 2,
            Format = Format.R8G8B8A8_UNorm,
            Width = _win32Window.Width,
            Height = _win32Window.Height,
            SampleDescription = new(1, 0),
            Scaling = Scaling.Stretch,
            Stereo = false,
            SwapEffect = SwapEffect.Discard,
            BufferUsage = Usage.RenderTargetOutput
        };

        // Obtain instance of the IDXGIDevice3 interface from the Direct3D device.
        IDXGIDevice3 dxgiDevice3 = _device.QueryInterface<IDXGIDevice3>();
        // Obtain instance of the IDXGIFactory2 interface from the DXGI device.
        IDXGIFactory2 dxgiFactory2 = dxgiDevice3.GetAdapter().GetParent<IDXGIFactory2>();
        // Creates a swap chain using the swap chain description.
        IDXGISwapChain1 swapChain1 = dxgiFactory2.CreateSwapChainForHwnd(dxgiDevice3, _win32Window.Handle, swapChainDescription1);

        _data.SwapChain = swapChain1.QueryInterface<IDXGISwapChain2>();
        _data.SwapChain.BackgroundColor = new Color4(0, 0, 0, 0);

        _data.RenderTargetTexture = _data.SwapChain.GetBuffer<ID3D11Texture2D>(0);
        _data.RenderTargetView = _device.CreateRenderTargetView(_data.RenderTargetTexture);
    }

    public void Update(IntPtr imGuiContext, Vector2 newSize)
    {
        ImGui.SetCurrentContext(imGuiContext);
        var io = ImGui.GetIO();

        io.DeltaTime = Time.DeltaF;
        io.DisplaySize = newSize;

        ImGui.NewFrame();
    }

    public void Present() =>
        // Present the final render to the screen.
        _data.SwapChain.Present(_data.VSync ? 1 : 0, PresentFlags.None);

    public void Render()
    {
        // Set the background color to a dark gray.
        var col = new Color4(0.1f, 0.1f, 0.1f, 0);

        _deviceContext.ClearRenderTargetView(_data.RenderTargetView, col);
        _deviceContext.OMSetRenderTargets(_data.RenderTargetView);
        _deviceContext.RSSetViewport(0, 0, _win32Window.Width, _win32Window.Height);

        Draw(ImGui.GetDrawData());
    }

    private void Draw(ImDrawDataPtr data)
    {
        // Avoid rendering when minimized
        if (data.DisplaySize.X <= 0.0f || data.DisplaySize.Y <= 0.0f)
            return;

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

        // Upload vertex/index data into a single contiguous GPU buffer
        var vertexResource = _deviceContext.Map(_data.VertexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        var indexResource = _deviceContext.Map(_data.IndexBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
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
        _deviceContext.Unmap(_data.VertexBuffer, 0);
        _deviceContext.Unmap(_data.IndexBuffer, 0);

        // Setup orthographic projection matrix into our constant buffer
        // Our visible imGui space lies from draw_data.DisplayPos (top left) to draw_data.DisplayPos+data_data.DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.

        var constResource = _deviceContext.Map(_data.ConstantBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
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
        _deviceContext.Unmap(_data.ConstantBuffer, 0);

        SetupRenderState(data);

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
                    _deviceContext.RSSetScissorRects(new[] { rect });

                    _textureResources.TryGetValue(cmd.TextureId, out var texture);
                    if (texture != null)
                        _deviceContext.PSSetShaderResources(0, new[] { texture });

                    _deviceContext.DrawIndexed((int)cmd.ElemCount, (int)(cmd.IdxOffset + global_idx_offset), (int)(cmd.VtxOffset + global_vtx_offset));
                }
            }
            global_idx_offset += cmdList.IdxBuffer.Size;
            global_vtx_offset += cmdList.VtxBuffer.Size;
        }
    }

    private void SetupRenderState(ImDrawDataPtr drawData)
    {
        _data.SetupViewports(new Viewport
        {
            Width = drawData.DisplaySize.X,
            Height = drawData.DisplaySize.Y,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        });

        _data.PrimitiveTopology = PrimitiveTopology.TriangleList;
        _data.SetupRenderState(sizeof(ImDrawVert), 0, sizeof(ImDrawIdx) == 2 ? Format.R16_UInt : Format.R32_UInt);
    }

    private void CreateMaterial()
    {
        ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode("ImGui.hlsl", "VS", "vs_4_0");
        _data.VertexShader = _device.CreateVertexShader(vertexShaderByteCode.Span);

        var inputElements = new[]
        {
                new InputElementDescription( "POSITION", 0, Format.R32G32_Float,   0, 0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "TEXCOORD", 0, Format.R32G32_Float,   8,  0, InputClassification.PerVertexData, 0 ),
                new InputElementDescription( "COLOR",    0, Format.R8G8B8A8_UNorm, 16, 0, InputClassification.PerVertexData, 0 ),
        };
        _data.InputLayout = _device.CreateInputLayout(inputElements, vertexShaderByteCode.Span);

        BufferDescription constBufferDesc = new()
        {
            ByteWidth = _vertexConstantBufferSize,
            Usage = ResourceUsage.Dynamic,
            BindFlags = BindFlags.ConstantBuffer,
            CPUAccessFlags = CpuAccessFlags.Write
        };
        _data.ConstantBuffer = _device.CreateBuffer(constBufferDesc);

        ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode("ImGui.hlsl", "PS", "ps_4_0");
        _data.PixelShader = _device.CreatePixelShader(pixelShaderByteCode.Span);

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
        _data.ResourceView = _device.CreateShaderResourceView(texture, resViewDesc);
        texture.Release();

        io.Fonts.TexID = RegisterTexture(_data.ResourceView);

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
        _data.SamplerState = _device.CreateSamplerState(samplerDesc);
    }

    public IntPtr RegisterTexture(ID3D11ShaderResourceView texture)
    {
        var id = texture.NativePointer;
        _textureResources.Add(id, texture);

        return id;
    }

    public void Resize()
    {
        if (!IsRendering)
            return;

        _data.RenderTargetView.Dispose();
        _data.RenderTargetTexture.Dispose();

        _data.SwapChain.ResizeBuffers(1, _win32Window.Width, _win32Window.Height, Format.R8G8B8A8_UNorm, SwapChainFlags.None);

        _data.RenderTargetTexture = _data.SwapChain.GetBuffer<ID3D11Texture2D1>(0);
        _data.RenderTargetView = _device.CreateRenderTargetView(_data.RenderTargetTexture);
    }

    public void Dispose()
    {
        if (_device is null)
            return;

        InvalidateDeviceObjects();

        _device?.Release();
        _deviceContext?.Release();
    }

    private void InvalidateDeviceObjects()
    {
        _data.SamplerState?.Release();
        _data.ResourceView?.Release();
        _data.IndexBuffer?.Release();
        _data.VertexBuffer?.Release();
        _data.BlendState?.Release();
        _data.DepthStencilState?.Release();
        _data.RasterizerState?.Release();
        _data.PixelShader?.Release();
        _data.ConstantBuffer?.Release();
        _data.InputLayout?.Release();
        _data.VertexShader?.Release();
    }

    private static ReadOnlyMemory<byte> CompileBytecode(string shaderName, string entryPoint, string profile)
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
