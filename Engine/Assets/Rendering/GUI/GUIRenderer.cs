using Vortice.Direct3D12;
using Vortice.Dxc;
using Vortice.DXGI;

using ImDrawIdx = System.UInt16;

namespace Engine.GUI;

public unsafe sealed partial class GUIRenderer
{
    public CommonContext Context;

    public InputLayoutDescription InputLayoutDescription;

    public Texture2D FontTexture;
    public MeshInfo GUIMesh;

    public PipelineStateObjectDescription PipelineStateObjectDescription = new()
    {
        InputLayout = "ImGui",
        CullMode = CullMode.None,
        RenderTargetFormat = Format.R8G8B8A8_UNorm,
        RenderTargetCount = 1,
        PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
        BlendState = "Alpha",
    };

    public void Initialize()
    {
        Context.Kernel.GUIContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(Context.Kernel.GUIContext);

        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset; // We can honor the ImDrawCmd::VtxOffset field, allowing for large meshes.

        FontTexture = new Texture2D();
        Context.RenderTargets["ImGui Font"] = FontTexture;

        //ImFontPtr font = io.Fonts.AddFontFromFileTTF("c:\\Windows\\Fonts\\SIMHEI.ttf", 14, null, io.Fonts.GetGlyphRangesChineseFull());

        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);
        io.Fonts.TexID = Context.GetIDFromString("ImGui Font");

        FontTexture.Width = width;
        FontTexture.Height = height;
        FontTexture.MipLevels = 1;
        FontTexture.Format = Format.R8G8B8A8_UNorm;
        GUIMesh = Context.GetMesh("ImGui Mesh");

        GPUUpload gpuUpload = new();
        gpuUpload.Texture2D = FontTexture;
        gpuUpload.Format = Format.R8G8B8A8_UNorm;
        gpuUpload.TextureData = new byte[width * height * bytesPerPixel];

        Span<byte> data = new(pixels, gpuUpload.TextureData.Length);
        data.CopyTo(gpuUpload.TextureData);

        Context.UploadQueue.Enqueue(gpuUpload);
    }

    public void Update(IntPtr context)
    {
        ImGui.SetCurrentContext(context);
        ImGui.GetIO().DisplaySize = Context.GraphicsDevice.Size.ToVector2();

        ImGui.NewFrame();

        ImGui.GetIO().DeltaTime = Time.DeltaF;
    }

    public void Render()
    {
        ImGui.ShowDemoWindow();

        ImGui.Render();

        RenderImDrawData();
    }

    private string _profiler = string.Empty;
    private string _output = string.Empty;
    public void ProfileWindows()
    {
        ImGui.SetNextWindowBgAlpha(0.35f);
        if (ImGui.Begin("Profiler", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (Time.OnFixedFrame)
                _profiler = Profiler.GetAdditionalString();
            ImGui.Text(_profiler);
            ImGui.End();
        }

        ImGui.SetNextWindowBgAlpha(0.35f);
        if (ImGui.Begin("Output", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (Output.GetLogs.Count > 0)
                _output = Output.DequeueLog()?.GetString() + _output;
            ImGui.Text(_output);
            ImGui.End();
        }
    }
}

public unsafe sealed partial class GUIRenderer
{
    public void LoadDefaultResource()
    {
        string directoryPath = AppContext.BaseDirectory + @"Assets\Resources\Shaders\";

        Context.VertexShaders["ImGui"] = Context.GraphicsContext.LoadShader(DxcShaderStage.Vertex, directoryPath + "ImGui.hlsl", "VS");
        Context.PixelShaders["ImGui"] = Context.GraphicsContext.LoadShader(DxcShaderStage.Pixel, directoryPath + "ImGui.hlsl", "PS");
        Context.PipelineStateObjects["ImGui"] = new PipelineStateObject(Context.VertexShaders["ImGui"], Context.PixelShaders["ImGui"]); ;
    }

    private void RenderImDrawData()
    {
        var data = ImGui.GetDrawData();
        var graphicsContext = Context.GraphicsContext;

        graphicsContext.SetRootSignature(Context.CreateRootSignatureFromString("Cs"));
        graphicsContext.SetPipelineState(Context.PipelineStateObjects["ImGui"], PipelineStateObjectDescription);

        graphicsContext.CommandList.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);

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
        int index = Context.UploadBuffer.Upload<float>(mvp);
        Context.UploadBuffer.SetConstantBufferView(graphicsContext, index, 0);

        Vector2 clipOffset = data.DisplayPos;
        for (int i = 0; i < data.CmdListsCount; i++)
        {
            var commandList = data.CmdListsRange[i];

            var vertexBytes = commandList.VtxBuffer.Size * sizeof(ImDrawVert);
            var indexBytes = commandList.IdxBuffer.Size * sizeof(ImDrawIdx);

            Context.UploadBuffer.UploadMeshIndex(graphicsContext, GUIMesh, new Span<byte>(commandList.IdxBuffer.Data.ToPointer(), indexBytes), Format.R16_UInt);
            Context.UploadBuffer.UploadVertexBuffer(graphicsContext, ref GUIMesh.VertexBufferResource, new Span<byte>(commandList.VtxBuffer.Data.ToPointer(), vertexBytes));

            GUIMesh.Vertices["POSITION"] = new VertexBuffer() { Offset = 0, Resource = GUIMesh.VertexBufferResource, SizeInByte = vertexBytes, Stride = sizeof(ImDrawVert) };
            GUIMesh.Vertices["TEXCOORD"] = new VertexBuffer() { Offset = 8, Resource = GUIMesh.VertexBufferResource, SizeInByte = vertexBytes, Stride = sizeof(ImDrawVert) };
            GUIMesh.Vertices["COLOR"] = new VertexBuffer() { Offset = 16, Resource = GUIMesh.VertexBufferResource, SizeInByte = vertexBytes, Stride = sizeof(ImDrawVert) };

            graphicsContext.SetMesh(GUIMesh);

            for (int j = 0; j < commandList.CmdBuffer.Size; j++)
            {
                var cmd = commandList.CmdBuffer[j];

                if (cmd.UserCallback != IntPtr.Zero)
                    throw new NotImplementedException("user callbacks not implemented");
                else
                {
                    graphicsContext.SetShaderResourceView(Context.GetTextureByStringID(cmd.TextureId), 0);

                    var rect = new Vortice.RawRect((int)(cmd.ClipRect.X - clipOffset.X), (int)(cmd.ClipRect.Y - clipOffset.Y), (int)(cmd.ClipRect.Z - clipOffset.X), (int)(cmd.ClipRect.W - clipOffset.Y));
                    graphicsContext.CommandList.RSSetScissorRects(new[] { rect });

                    graphicsContext.DrawIndexedInstanced((int)cmd.ElemCount, 1, (int)(cmd.IdxOffset), (int)(cmd.VtxOffset), 0);
                }
            }
        }
    }
}