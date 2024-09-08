using Vortice.DXGI;

namespace Engine.GUI;

public unsafe sealed partial class GUIRenderer
{
    public RootSignature RootSignature;

    public MeshData GUIMesh;
    public Texture2D FontTexture;

    public CommonContext Context => _context ??= Kernel.Instance.Context;
    public CommonContext _context;

    public PipelineStateObjectDescription PipelineStateObjectDescription = new()
    {
        InputLayout = "ImGui",
        BlendState = "Alpha",
    };

    public void Initialize()
    {
        Context.Kernel.GUIContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(Context.Kernel.GUIContext);

        LoadResources();
        LoadTexture();
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
                _output = Output.DequeueLogs() + _output;

            ImGui.Text(_output);
            ImGui.End();
        }
    }
}

public unsafe sealed partial class GUIRenderer
{
    public void LoadResources()
    {
        Assets.VertexShaders["ImGui"] = Context.GraphicsContext.LoadShader(DxcShaderStage.Vertex, Paths.SHADERS + "ImGui.hlsl", "VS");
        Assets.PixelShaders["ImGui"] = Context.GraphicsContext.LoadShader(DxcShaderStage.Pixel, Paths.SHADERS + "ImGui.hlsl", "PS");

        Assets.PipelineStateObjects["ImGui"] = new PipelineStateObject(Assets.VertexShaders["ImGui"], Assets.PixelShaders["ImGui"]);

        RootSignature = Context.CreateRootSignatureFromString("Cs");
    }

    public void LoadTexture()
    {
        var io = ImGui.GetIO();

        //ImFontPtr font = io.Fonts.AddFontFromFileTTF("c:\\Windows\\Fonts\\SIMHEI.ttf", 14, null, io.Fonts.GetGlyphRangesChineseFull());

        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);
        io.Fonts.TexID = Context.GetIDFromString("ImGui Font");

        FontTexture = new()
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            Format = Format.R8G8B8A8_UNorm,
        };
        Assets.RenderTargets["ImGui Font"] = FontTexture;

        GPUUpload upload = new()
        {
            Texture2D = FontTexture,
            TextureData = new byte[width * height * bytesPerPixel],
        };

        Span<byte> data = new(pixels, upload.TextureData.Length);
        data.CopyTo(upload.TextureData);

        Context.UploadQueue.Enqueue(upload);
    }

    private void RenderImDrawData()
    {
        var data = ImGui.GetDrawData();

        if (data.DisplaySize.X < 1
         || data.DisplaySize.Y < 1)
            return;

        Context.GraphicsContext.SetRootSignature(RootSignature);
        Context.GraphicsContext.SetPipelineState(Assets.PipelineStateObjects["ImGui"], PipelineStateObjectDescription);

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
        Context.UploadBuffer.Upload<float>(mvp, out var offset);
        Context.UploadBuffer.SetConstantBufferView(offset, 0);

        Vector2 clipOffset = data.DisplayPos;
        for (int i = 0; i < data.CmdListsCount; i++)
        {
            var commandList = data.CmdListsRange[i];

            var meshData = Context.CreateMesh($"ImGui Mesh {i}", inputLayoutElements: "ptc", indexFormat16Bit: true);

            var indexBytes = commandList.IdxBuffer.Size * meshData.IndexStride;
            var vertexBytes = commandList.VtxBuffer.Size * meshData.VertexStride;

            Context.UploadBuffer.UploadIndexBuffer(meshData, new Span<byte>(commandList.IdxBuffer.Data.ToPointer(), indexBytes), meshData.IndexFormat);
            Context.UploadBuffer.UploadVertexBuffer(meshData, new Span<byte>(commandList.VtxBuffer.Data.ToPointer(), vertexBytes));

            Context.GraphicsContext.SetMesh(meshData);

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