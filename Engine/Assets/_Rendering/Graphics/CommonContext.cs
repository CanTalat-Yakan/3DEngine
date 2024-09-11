using System.Collections.Concurrent;
using System.Collections.Generic;

using Vortice.Direct3D12;
using Vortice.DXGI;

using System.IO;
using System.Linq;
using Vortice.Mathematics;

namespace Engine.Graphics;

public sealed partial class CommonContext : IDisposable
{
    public bool IsRendering => GraphicsDevice.RenderTextureViewHeap is not null;

    public Kernel Kernel { get; private set; }

    public GraphicsDevice GraphicsDevice = new();
    public GraphicsContext GraphicsContext = new();

    public Dictionary<IntPtr, string> PointerToString = new();
    public Dictionary<string, IntPtr> StringToPointer = new();

    public ConcurrentQueue<GPUUpload> UploadQueue = new();
    public RingUploadBuffer UploadBuffer = new();

    public CommonContext(Kernel kernel) =>
        Kernel = kernel;

    public void LoadAssets(string[] filePaths = null)
    {
        if (string.IsNullOrEmpty(EditorState.AssetsPath) || !Directory.Exists(EditorState.AssetsPath))
            return;

        if (filePaths is null)
            filePaths = Directory.EnumerateFiles(EditorState.AssetsPath, "*", SearchOption.AllDirectories).ToArray();

        foreach (var filePath in filePaths)
        {
            var fileInfo = new FileInfo(filePath);

            var guid = Guid.NewGuid();
            var name = Path.GetFileNameWithoutExtension(filePath);
            var extension = fileInfo.Extension;
            var localPath = Path.GetRelativePath(EditorState.AssetsPath, fileInfo.FullName);

            Assets.Metadata.Add(guid, new AssetEntry(name, extension, localPath));
        }
    }

    public void LoadDefaultResources()
    {
        CreateShaderFromResources(["Unlit", "SimpleLit", "Sky"]);

        ModelLoader.LoadFile(AssetsPaths.PRIMITIVES + "Cube.obj");
        ModelLoader.LoadFile(AssetsPaths.PRIMITIVES + "Sphere.obj");

        ImageLoader.LoadTexture(AssetsPaths.TEXTURES + "Default.png");
        ImageLoader.LoadTexture(AssetsPaths.TEXTURES + "SkyGradient.png");
        ImageLoader.LoadTexture(AssetsPaths.TEXTURES + "Transparent.png");
        ImageLoader.LoadTexture(AssetsPaths.TEXTURES + "UVMap.png");
    }

    public void Dispose()
    {
        Assets.Dispose();

        UploadBuffer?.Dispose();

        GraphicsContext.Dispose();
        GraphicsDevice.Dispose();
    }

}

public sealed partial class CommonContext : IDisposable
{
    public Texture2D GetTextureByString(string name) =>
        Assets.RenderTargets[GetStringFromID(GetIDFromString(name))];

    public Texture2D GetTextureByStringID(nint pointer) =>
        Assets.RenderTargets[GetStringFromID(pointer)];

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

    public string GetStringFromID(nint pointer)
    {
        if (!PointerToString.ContainsKey(pointer))
            throw new NotImplementedException("error string from id in common context");

        return PointerToString[pointer];
    }
}

public sealed partial class CommonContext : IDisposable
{
    public void CreateShaderFromResources(params string[] shaderNameList)
    {
        foreach (string shaderName in shaderNameList)
        {
            Assets.VertexShaders[shaderName] = GraphicsContext.LoadShader(DxcShaderStage.Vertex, AssetsPaths.SHADERS + shaderName + ".hlsl", "VS");
            Assets.PixelShaders[shaderName] = GraphicsContext.LoadShader(DxcShaderStage.Pixel, AssetsPaths.SHADERS + shaderName + ".hlsl", "PS");
            Assets.PipelineStateObjects[shaderName] = new PipelineStateObject(Assets.VertexShaders[shaderName], Assets.PixelShaders[shaderName]);
        }
    }

    public void CreateShader(params string[] shaderPathList)
    {
        foreach (string shaderPath in shaderPathList)
        {
            Assets.VertexShaders[shaderPath] = GraphicsContext.LoadShader(DxcShaderStage.Vertex, shaderPath + ".hlsl", "VS");
            Assets.PixelShaders[shaderPath] = GraphicsContext.LoadShader(DxcShaderStage.Pixel, shaderPath + ".hlsl", "PS");
            Assets.PipelineStateObjects[shaderPath] = new PipelineStateObject(Assets.VertexShaders[shaderPath], Assets.PixelShaders[shaderPath]);
        }
    }

    public MeshData CreateMesh(string name, string inputLayoutElements = "PNTt", bool indexFormat16Bit = true)
    {
        if (Assets.Meshes.TryGetValue(name, out MeshData mesh))
            return mesh;
        else
        {
            mesh = new();
            mesh.Name = name;
            mesh.InputLayoutDescription = CreateInputLayoutDescription(inputLayoutElements);

            mesh.IndexFormat = indexFormat16Bit ? Format.R16_UInt : Format.R32_UInt;
            mesh.IndexStride = GraphicsDevice.GetSizeInByte(mesh.IndexFormat);

            var offset = 0;
            foreach (var inputElement in mesh.InputLayoutDescription.Elements)
            {
                mesh.VertexStride += GraphicsDevice.GetSizeInByte(inputElement.Format);
                mesh.Vertices[inputElement.SemanticName] = new() { Offset = offset, };
                offset += GraphicsDevice.GetSizeInByte(inputElement.Format);
            }

            foreach (var inputElement in mesh.InputLayoutDescription.Elements)
                mesh.Vertices[inputElement.SemanticName].Stride = mesh.VertexStride;

            Assets.Meshes[name] = mesh;

            return mesh;
        }
    }

    public MeshData CreateMeshData(List<int> indices, List<float> vertices, List<Vector3> positions, string meshName = null, string inputLayoutElements = null) =>
        CreateMeshData(meshName ??= Guid.NewGuid().ToString(), inputLayoutElements ??= "PNTt", indices, vertices, positions);
    
    public MeshData CreateMeshData(string meshName, string inputLayoutElements, List<int> indices, List<Vertex> vertices, List<Vector3> positions) =>
        CreateMeshData(meshName, inputLayoutElements, indices, vertices.ToFloats(), positions);

    public MeshData CreateMeshData(string meshName, string inputLayoutElements, List<int> indices, List<float> vertices, List<Vector3> positions)
    {
        var meshData = CreateMesh(meshName, inputLayoutElements);
        meshData.IndexCount = indices.Count;
        meshData.VertexCount = positions.Count;
        meshData.BoundingBox = BoundingBox.CreateFromPoints(positions.ToArray());

        GPUUpload upload = new()
        {
            MeshData = meshData,
            VertexData = vertices.ToArray(),
            IndexData = indices.ToArray(),
            IndexFormat = Format.R32_UInt,
        };
        UploadQueue.Enqueue(upload);

        return meshData;
    }

    public enum InputLayoutElements
    {
        Position3D,
        Position2D,
        Normal,
        Tangent,
        ColorRGBA,
        ColorSRGBA,
        UV
    }

    public InputLayoutDescription CreateInputLayoutDescription(params InputLayoutElements[] elements)
    {
        List<char> inputLayoutElements = new();

        foreach (var element in elements)
        {
            inputLayoutElements.Add(element switch
            {
                InputLayoutElements.Position3D => 'P',
                InputLayoutElements.Position2D => 'p',
                InputLayoutElements.Normal => 'N',
                InputLayoutElements.Tangent => 'T',
                InputLayoutElements.UV => 't',
                InputLayoutElements.ColorRGBA => 'C',
                InputLayoutElements.ColorSRGBA => 'c',
                _ => throw new NotImplementedException(),
            });
        }

        return CreateInputLayoutDescription(inputLayoutElements.ToString());
    }

    public InputLayoutDescription CreateInputLayoutDescription(string inputLayoutElements)
    {
        if (Assets.InputLayoutDescriptions.TryGetValue(inputLayoutElements, out var inputLayout))
            return inputLayout;

        inputLayout = new InputLayoutDescription();
        Assets.InputLayoutDescriptions[inputLayoutElements] = inputLayout;
        var description = new InputElementDescription[inputLayoutElements.Length];

        for (int i = 0; i < inputLayoutElements.Length; i++)
            description[i] = inputLayoutElements[i] switch
            {
                'P' => new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, i),
                'N' => new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, i),
                'T' => new InputElementDescription("TANGENT", 0, Format.R32G32B32_Float, i),
                'C' => new InputElementDescription("COLOR", 0, Format.R32G32B32A32_Float, i),

                'p' => new InputElementDescription("POSITION", 0, Format.R32G32_Float, i),
                't' => new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, i),
                'c' => new InputElementDescription("COLOR", 0, Format.R8G8B8A8_UNorm, i),
                _ => throw new NotImplementedException("error input element in common context"),
            };

        return inputLayout.Elements = description;
    }

    public RootSignature CreateRootSignatureFromString(string rootSignatureParameters)
    {
        if (Assets.RootSignatures.TryGetValue(rootSignatureParameters, out var rootSignature))
            return rootSignature;

        rootSignature = new RootSignature();
        Assets.RootSignatures[rootSignatureParameters] = rootSignature;
        var description = new RootSignatureParameters[rootSignatureParameters.Length];

        for (int i = 0; i < rootSignatureParameters.Length; i++)
            description[i] = rootSignatureParameters[i] switch
            {
                'C' => RootSignatureParameters.ConstantBufferView,
                'c' => RootSignatureParameters.ConstantBufferViewTable,
                'S' => RootSignatureParameters.ShaderResourceView,
                's' => RootSignatureParameters.ShaderResourceViewTable,
                'U' => RootSignatureParameters.UnorderedAccessView,
                'u' => RootSignatureParameters.UnorderedAccessViewTable,
                _ => throw new NotImplementedException("error root signature description in common context"),
            };

        GraphicsDevice.CreateRootSignature(rootSignature, description);

        return rootSignature;
    }

    public void GPUUploadData(GraphicsContext graphicsContext)
    {
        while (UploadQueue.TryDequeue(out var upload))
        {
            if (upload.MeshData is not null)
                graphicsContext.UploadMesh(
                    upload.MeshData,
                    upload.VertexData,
                    upload.IndexData,
                    upload.IndexFormat);

            if (upload.Texture2D is not null)
                graphicsContext.UploadTexture(
                    upload.Texture2D,
                    upload.TextureData);
        }
    }
}