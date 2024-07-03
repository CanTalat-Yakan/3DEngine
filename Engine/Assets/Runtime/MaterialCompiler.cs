using System.Collections.Generic;
using System.IO;

namespace Engine.Runtime;

public sealed class MaterialEntry(FileInfo fileInfo)
{
    public FileInfo FileInfo = fileInfo;

    public string ShaderName => ShaderEntry.FileInfo.Name.RemoveExtension();

    public ShaderEntry ShaderEntry;
    public Action OnShaderUpdate;

    public void SetShader(ShaderEntry shaderEntry)
    {
        if (shaderEntry is null)
            return;

        ShaderEntry = shaderEntry;

        var shaderName = ShaderName;

        Kernel.Instance.Context.CreateShader(shaderName);
        Assets.SerializableConstantBuffers[shaderName].SafeToSerializableConstantBuffer();

        MaterialCompiler.SetConstantBuffer(this, new() { ShaderName = shaderName });
        Serialization.SaveFile(Assets.SerializableConstantBuffers[shaderName], FileInfo.FullName);
    }
}

public class MaterialCompiler
{
    public void CompileProjectMaterials(string assetsPath = null)
    {
        if (assetsPath is null)
            return;

        string materialsFolderPath = Path.Combine(assetsPath, "Materials");
        if (!Directory.Exists(materialsFolderPath))
            return;

        foreach (var path in Directory.GetFiles(materialsFolderPath, "*", SearchOption.AllDirectories))
            CheckMaterialEntry(path);
    }

    private void CheckMaterialEntry(string path)
    {
        FileInfo fileInfo = new(path);

        if (Assets.Materials.TryGetValue(fileInfo.Name.RemoveExtension(), out var materialEntry))
        {
            var constantBuffer = Serialization.LoadFile<SerializableConstantBuffer>(path);

            if (string.IsNullOrEmpty(constantBuffer.ShaderName))
                return;

            materialEntry = new MaterialEntry(fileInfo);
            SetConstantBuffer(materialEntry, constantBuffer);

            constantBuffer.PasteToPropertiesConstantBuffer();

            Assets.Materials.Add(fileInfo.Name.RemoveExtension(), materialEntry);

            Output.Log("Read new Material");
        }
        else if (fileInfo.LastWriteTime > materialEntry.FileInfo.LastWriteTime)
        {
            materialEntry.FileInfo = fileInfo;

            var constantBuffer = Serialization.LoadFile<SerializableConstantBuffer>(path);

            SetConstantBuffer(materialEntry, constantBuffer);

            constantBuffer.PasteToPropertiesConstantBuffer();

            Output.Log("Updated Material");
        }
    }

    public static void SetConstantBuffer(MaterialEntry materialEntry, SerializableConstantBuffer constantBuffer)
    {
        var shaderEntry = Assets.Shaders[constantBuffer.ShaderName];

        materialEntry.ShaderEntry = shaderEntry;

        Assets.SerializableConstantBuffers[materialEntry.ShaderName].SetConstantBufferObject(constantBuffer);

        if (shaderEntry.ConstantBufferType is null)
        {
            Output.Log(
                "Could not create the MaterialBuffer PropertiesConstantBuffer, " +
                "because the ShaderEntry ConstantBufferType is null");
            return;
        }
    }
}