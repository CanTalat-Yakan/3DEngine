using System.Collections.Generic;

using Vortice.Direct3D12;

namespace Engine.Utilities;

public struct AssetEntry(string name, string extension, string localPath)
{
    public string Name = name;
    public string Extension = extension;
    public string LocalPath = localPath;
}

public sealed partial class Assets
{
    public static Dictionary<string, Type> Components = new();
    public static Dictionary<string, ScriptEntry> Scripts = new();

    public static Dictionary<string, MaterialEntry> Materials = new();
    public static Dictionary<string, ShaderEntry> Shaders = new();

    public static Dictionary<string, ReadOnlyMemory<byte>> VertexShaders = new();
    public static Dictionary<string, ReadOnlyMemory<byte>> PixelShaders = new();

    public static Dictionary<string, RootSignature> RootSignatures = new();
    public static Dictionary<string, InputLayoutDescription> InputLayoutDescriptions = new();
    public static Dictionary<string, PipelineStateObject> PipelineStateObjects = new();

    public static Dictionary<string, Texture2D> RenderTargets = new();
    public static Dictionary<string, MeshData> Meshes = new();

    public static Dictionary<string, SerializableConstantBuffer> SerializableConstantBuffers = new();

    public static Dictionary<Guid, AssetEntry> Metadata = new();

    public static void Dispose()
    {
        DisposeDictionaryItems(RootSignatures);
        DisposeDictionaryItems(PipelineStateObjects);
        DisposeDictionaryItems(RenderTargets);
        DisposeDictionaryItems(Meshes);

        Components.Clear();
        Scripts.Clear();
        Materials.Clear();
        Shaders.Clear();
        VertexShaders.Clear();
        PixelShaders.Clear();
        InputLayoutDescriptions.Clear();
        SerializableConstantBuffers.Clear();
    }

    private static void DisposeDictionaryItems<T1, T2>(Dictionary<T1, T2> dictionary) where T2 : IDisposable
    {
        foreach (var pair in dictionary)
            pair.Value.Dispose();

        dictionary.Clear();
    }
}

public sealed partial class Assets
{
    public static AssetEntry GetAssetMetadata(Guid guid)
    {
        if (Metadata.ContainsKey(guid))
            return Metadata[guid];

        return default;
    }

    public static AssetEntry GetAssetMetadata(string name)
    {
        foreach (var metadata in Metadata.Values)
            if(metadata.Name == name) 
                return metadata;

        return default;
    }
}