using System.Collections.Concurrent;
using System.Collections.Generic;

using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

using System.IO;
using System.Linq;

namespace Engine.Graphics;

public sealed partial class CommonContext : IDisposable
{
    public bool IsRendering => GraphicsDevice.BackBufferRenderTargetsViewHeap is not null;

    public Kernel Kernel { get; private set; }

    public GraphicsDevice GraphicsDevice = new();
    public GraphicsContext GraphicsContext = new();
    public ComputeContext ComputeContext = new();

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
        CreateShader(fromResources: true, ["Unlit", "SimpleLit", "Sky"]);

        ModelLoader.LoadFile(AssetPaths.PRIMITIVES + "Cube.obj");
        ModelLoader.LoadFile(AssetPaths.PRIMITIVES + "Sphere.obj");

        ImageLoader.LoadFile(AssetPaths.RESOURCETEXTURES + "Default.png");
        ImageLoader.LoadFile(AssetPaths.RESOURCETEXTURES + "Transparent.png");
        ImageLoader.LoadFile(AssetPaths.RESOURCETEXTURES + "UVMap.png");
    }

    public void Dispose()
    {
        Assets.Dispose();

        GraphicsContext.Dispose();
        GraphicsDevice.Dispose();
        ComputeContext.Dispose();

        UploadBuffer?.Dispose();

        foreach (var gpuUpload in UploadQueue)
            gpuUpload.Texture2D?.Dispose();

        UploadQueue?.Clear();

        GC.SuppressFinalize(this);
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
    public void CreateShader(bool fromResources = false, params string[] localPaths)
    {
        foreach (string shaderPath in localPaths)
        {
            string shaderName = shaderPath.GetFileNameWithoutExtension();

            Assets.VertexShaders[shaderName] = GraphicsContext.LoadShader(DxcShaderStage.Vertex, shaderPath.RemoveExtension() + ".hlsl", "VS", fromResources);
            Assets.PixelShaders[shaderName] = GraphicsContext.LoadShader(DxcShaderStage.Pixel, shaderPath.RemoveExtension() + ".hlsl", "PS", fromResources);
            Assets.PipelineStateObjects[shaderName] = new(Assets.VertexShaders[shaderName], Assets.PixelShaders[shaderName]);
        }
    }

    public void CreateComputeShader(bool fromResources = false, params string[] localPaths)
    {
        foreach (string computeShaderPath in localPaths)
        {
            string computeShaderName = computeShaderPath.GetFileNameWithoutExtension();

            Assets.ComputeShaders[computeShaderName] = ComputeContext.LoadComputeShader(DxcShaderStage.Compute, computeShaderPath.RemoveExtension() + ".hlsl", "CS", fromResources);
            Assets.ComputePipelineStateObjects[computeShaderName] = new(Assets.ComputeShaders[computeShaderName]);
        }
    }

    public MeshData CreateMesh(InputLayoutHelper inputLayoutHelper, string name = null, bool indexFormat16Bit = true) =>
        CreateMesh(name, inputLayoutHelper.GetString(), indexFormat16Bit);

    public MeshData CreateMesh(string name = null, string inputLayoutElements = "PNTt", bool indexFormat16Bit = true)
    {
        if (!string.IsNullOrEmpty(name) && Assets.Meshes.TryGetValue(name, out var meshData))
            return meshData;
        else
        {
            meshData = new();

            meshData.Name = name;
            meshData.InputLayoutDescription = CreateInputLayoutDescription(inputLayoutElements);

            meshData.IndexFormat = indexFormat16Bit ? Format.R16_UInt : Format.R32_UInt;
            meshData.IndexStride = (uint)GraphicsDevice.GetSizeInByte(meshData.IndexFormat);

            uint offset = 0;
            foreach (var inputElement in meshData.InputLayoutDescription.Elements)
            {
                meshData.VertexStride += (uint)GraphicsDevice.GetSizeInByte(inputElement.Format);
                meshData.Vertices[inputElement.SemanticName] = new() { Offset = offset, };
                offset += (uint)GraphicsDevice.GetSizeInByte(inputElement.Format);
            }

            foreach (var inputElement in meshData.InputLayoutDescription.Elements)
                meshData.Vertices[inputElement.SemanticName].Stride = meshData.VertexStride;

            if (!string.IsNullOrEmpty(name))
                Assets.Meshes[name] = meshData;

            return meshData;
        }
    }

    public MeshData CreateMeshData(float[] vertices, int[] indices, Vector3[] positions, InputLayoutHelper inputLayoutHelper, string meshName = null, bool unsignedInt32IndexFormat = true) =>
        CreateMeshData(vertices, indices, positions, meshName, inputLayoutHelper.GetString(), unsignedInt32IndexFormat);

    public MeshData CreateMeshData(float[] vertices, int[] indices, Vector3[] positions, string meshName = null, string inputLayoutElements = null, bool unsignedInt32IndexFormat = true)
    {
        inputLayoutElements ??= "PNTt";

        var meshData = CreateMesh(meshName, inputLayoutElements);
        meshData.VertexCount = (uint)vertices.Length;
        meshData.IndexCount = (uint)indices.Length;
        meshData.BoundingBox = BoundingBox.CreateFromPoints(positions);

        GPUUpload upload = new()
        {
            MeshData = meshData,
            VertexData = vertices,
            IndexData = indices,
            IndexFormat = unsignedInt32IndexFormat ? Format.R32_UInt : Format.R16_UInt,
        };
        UploadQueue.Enqueue(upload);

        return meshData;
    }

    public InputLayoutDescription CreateInputLayoutDescription(string inputLayoutElements)
    {
        if (Assets.InputLayoutDescriptions.TryGetValue(inputLayoutElements, out var inputLayout))
            return inputLayout;

        inputLayout = new InputLayoutDescription();
        Assets.InputLayoutDescriptions[inputLayoutElements] = inputLayout;
        var description = new InputElementDescription[inputLayoutElements.Length];

        for (uint i = 0; i < inputLayoutElements.Length; i++)
            description[i] = inputLayoutElements[(int)i] switch
            {
                'f' => new InputElementDescription("POSITION", 0, Format.R32_Float, i),
                'p' => new InputElementDescription("POSITION", 0, Format.R32G32_Float, i),
                'P' => new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, i),
                't' => new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, i),
                'T' => new InputElementDescription("TANGENT", 0, Format.R32G32B32_Float, i),
                'N' => new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, i),
                'c' => new InputElementDescription("COLOR", 0, Format.R8G8B8A8_UNorm, i),
                'C' => new InputElementDescription("COLOR", 0, Format.R32G32B32A32_Float, i),

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

    public void GPUUploadData()
    {
        while (UploadQueue.TryDequeue(out var upload))
        {
            if (upload.MeshData is not null)
                GraphicsContext.UploadMesh(
                    upload.MeshData,
                    upload.VertexData,
                    upload.IndexData,
                    upload.IndexFormat);

            if (upload.Texture2D is not null)
                GraphicsContext.UploadTexture(
                    upload.Texture2D,
                    upload.TextureData);
        }
    }
}