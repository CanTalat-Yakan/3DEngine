using System.Collections.Generic;

using Vortice.Direct3D12;

namespace Engine.Utilities;

public sealed class Assets
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
    public static Dictionary<string, MeshInfo> Meshes = new();

    public static Dictionary<string, SerializableConstantBuffer> SerializableConstantBuffers = new();

    public static void Dispose()
    {
        DisposeDictionaryItems(RootSignatures);
        DisposeDictionaryItems(PipelineStateObjects);
        DisposeDictionaryItems(RenderTargets);
        DisposeDictionaryItems(Meshes);
    }

    private static void DisposeDictionaryItems<T1, T2>(Dictionary<T1, T2> dictionary) where T2 : IDisposable
    {
        foreach (var pair in dictionary)
            pair.Value.Dispose();

        dictionary.Clear();
    }
}
