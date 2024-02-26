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

        GPUUpload upload = new();
        upload.Texture2D = FontTexture;
        upload.Format = Format.R8G8B8A8_UNorm;
        upload.TextureData = new byte[width * height * bytesPerPixel];

        Span<byte> data = new(pixels, upload.TextureData.Length);
        data.CopyTo(upload.TextureData);

        Context.UploadQueue.Enqueue(upload);
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

        Context.GraphicsContext.SetRootSignature(Context.CreateRootSignatureFromString("Cs"));
        Context.GraphicsContext.SetPipelineState(Context.PipelineStateObjects["ImGui"], PipelineStateObjectDescription);

        Context.GraphicsContext.CommandList.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);

        if (Kernel.Instance.Config.ResolutionScale > 1)
            data.DisplaySize /= (float)Kernel.Instance.Config.ResolutionScale; // <--- SCALING

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
        Context.UploadBuffer.SetConstantBufferView(Context.GraphicsContext, index, 0);

        Vector2 clipOffset = data.DisplayPos;
        for (int i = 0; i < data.CmdListsCount; i++)
        {
            var commandList = data.CmdListsRange[i];

            var vertexBytes = commandList.VtxBuffer.Size * sizeof(ImDrawVert);
            var indexBytes = commandList.IdxBuffer.Size * sizeof(ImDrawIdx);

            Context.UploadBuffer.UploadMeshIndex(Context.GraphicsContext, GUIMesh, new Span<byte>(commandList.IdxBuffer.Data.ToPointer(), indexBytes), Format.R16_UInt);
            Context.UploadBuffer.UploadVertexBuffer(Context.GraphicsContext, ref GUIMesh.VertexBufferResource, new Span<byte>(commandList.VtxBuffer.Data.ToPointer(), vertexBytes));

            GUIMesh.Vertices["POSITION"] = new VertexBuffer() { Offset = 0, Resource = GUIMesh.VertexBufferResource, SizeInByte = vertexBytes, Stride = sizeof(ImDrawVert) };
            GUIMesh.Vertices["TEXCOORD"] = new VertexBuffer() { Offset = 8, Resource = GUIMesh.VertexBufferResource, SizeInByte = vertexBytes, Stride = sizeof(ImDrawVert) };
            GUIMesh.Vertices["COLOR"] = new VertexBuffer() { Offset = 16, Resource = GUIMesh.VertexBufferResource, SizeInByte = vertexBytes, Stride = sizeof(ImDrawVert) };

            Context.GraphicsContext.SetMesh(GUIMesh);

            for (int j = 0; j < commandList.CmdBuffer.Size; j++)
            {
                var cmd = commandList.CmdBuffer[j];

                if (cmd.UserCallback != IntPtr.Zero)
                    throw new NotImplementedException("user callbacks not implemented");
                else
                {
                    if (Kernel.Instance.Config.ResolutionScale > 1)
                        cmd.ClipRect *= (float)Kernel.Instance.Config.ResolutionScale; // <--- SCALING

                    Context.GraphicsContext.CommandList.RSSetScissorRects(new Vortice.RawRect(
                        (int)(cmd.ClipRect.X - clipOffset.X),
                        (int)(cmd.ClipRect.Y - clipOffset.Y),
                        (int)(cmd.ClipRect.Z - clipOffset.X),
                        (int)(cmd.ClipRect.W - clipOffset.Y)));

                    Context.GraphicsContext.SetShaderResourceView(Context.GetTextureByStringID(cmd.TextureId), 0);

                    Context.GraphicsContext.DrawIndexedInstanced((int)cmd.ElemCount, 1, (int)(cmd.IdxOffset), (int)(cmd.VtxOffset), 0);
                }
            }
        }
    }
}