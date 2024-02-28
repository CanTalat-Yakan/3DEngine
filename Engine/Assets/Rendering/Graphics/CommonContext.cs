using System.Collections.Concurrent;
using System.Collections.Generic;

using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Engine.Graphics;

public sealed partial class CommonContext : IDisposable
{
    public bool IsRendering => GraphicsDevice.RenderTextureViewHeap is not null;

    public Kernel Kernel { get; private set; }

    public GraphicsDevice GraphicsDevice = new();
    public GraphicsContext GraphicsContext = new();

    public Dictionary<string, ReadOnlyMemory<byte>> VertexShaders = new();
    public Dictionary<string, ReadOnlyMemory<byte>> PixelShaders = new();
    public Dictionary<string, RootSignature> RootSignatures = new();
    public Dictionary<string, MeshInfo> Meshes = new();
    public Dictionary<string, Texture2D> RenderTargets = new();
    public Dictionary<string, PipelineStateObject> PipelineStateObjects = new();

    public Dictionary<IntPtr, string> PointerToString = new();
    public Dictionary<string, IntPtr> StringToPointer = new();

    public ConcurrentQueue<GPUUpload> UploadQueue = new();
    public RingUploadBuffer UploadBuffer = new();

    public CommonContext(Kernel kernel) =>
        Kernel = kernel;

    public void Dispose()
    {
        UploadBuffer?.Dispose();
        DisposeDictionaryItems(RootSignatures);
        DisposeDictionaryItems(RenderTargets);
        DisposeDictionaryItems(PipelineStateObjects);
        DisposeDictionaryItems(Meshes);

        GraphicsContext.Dispose();
        GraphicsDevice.Dispose();
    }

    void DisposeDictionaryItems<T1, T2>(Dictionary<T1, T2> dictionary) where T2 : IDisposable
    {
        foreach (var pair in dictionary)
            pair.Value.Dispose();

        dictionary.Clear();
    }
}

public sealed partial class CommonContext : IDisposable
{
    private int somePointerValue = 65536;
    public IntPtr GetIDFromString(string name)
    {
        if (StringToPointer.TryGetValue(name, out IntPtr pointer))
            return pointer;

        pointer = new IntPtr(somePointerValue);

        StringToPointer[name] = pointer;
        PointerToString[pointer] = name;

        somePointerValue++;

        return pointer;
    }

    public string GetStringFromID(nint pointer) =>
        PointerToString[pointer];

    public Texture2D GetTextureByStringID(nint pointer) =>
        RenderTargets[GetStringFromID(pointer)];
}

public sealed partial class CommonContext : IDisposable
{
    public MeshInfo CreateMesh(string name, InputLayoutDescription inputLayoutDescription)
    {
        if (Meshes.TryGetValue(name, out MeshInfo mesh))
            return mesh;
        else
        {
            mesh = new();
            mesh.InputLayoutDescription = inputLayoutDescription;

            Meshes[name] = mesh;

            return mesh;
        }
    }

    public InputLayoutDescription CreateInputLayoutDescription(string s)
    {
        var description = new InputElementDescription[s.Length];

        for (int i = 0; i < s.Length; i++)
            description[i] = s[i] switch
            {
                'P' => new InputElementDescription("POSITION", 0, Format.R8G8B8A8_UNorm, i),
                'N' => new InputElementDescription("NORMAL", 0, Format.R8G8B8A8_UNorm, i),
                'T' => new InputElementDescription("TANGENT", 0, Format.R8G8B8A8_UNorm, i),
                'C' => new InputElementDescription("COLOR", 0, Format.R8G8B8A8_UNorm, i),

                't' => new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, i),
                'p' => new InputElementDescription("POSITION", 0, Format.R32G32_Float, i),
                _ => throw new NotImplementedException("error input element"),
            };

        return new(description);
    }

    public RootSignature CreateRootSignatureFromString(string s)
    {
        RootSignature rootSignature;
        if (RootSignatures.TryGetValue(s, out rootSignature))
            return rootSignature;

        rootSignature = new RootSignature();
        RootSignatures[s] = rootSignature;
        var description = new RootSignatureParameters[s.Length];

        for (int i = 0; i < s.Length; i++)
            description[i] = s[i] switch
            {
                'C' => RootSignatureParameters.ConstantBufferView,
                'c' => RootSignatureParameters.ConstantBufferViewTable,
                'S' => RootSignatureParameters.ShaderResourceView,
                's' => RootSignatureParameters.ShaderResourceViewTable,
                'U' => RootSignatureParameters.UnorderedAccessView,
                'u' => RootSignatureParameters.UnorderedAccessViewTable,
                _ => throw new NotImplementedException("error root signature description"),
            };

        GraphicsDevice.CreateRootSignature(rootSignature, description);

        return rootSignature;
    }

    public void GPUUploadData(GraphicsContext graphicsContext)
    {
        while (UploadQueue.TryDequeue(out var upload))
            if (upload.Texture2D is not null)
                graphicsContext.UploadTexture(upload.Texture2D, upload.TextureData);
    }
}