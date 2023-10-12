using System.Collections.Generic;
using System.IO;

namespace Engine.Runtime;

public sealed class MaterialEntry(FileInfo fileInfo)
{
    public FileInfo FileInfo = fileInfo;

    public Material Material;

    public ShaderEntry ShaderEntry;

    public void SetShader(string shaderName)
    {
        ShaderEntry = ShaderCompiler.ShaderCollector.GetShader(shaderName);

        if (ShaderEntry is null)
            return;

        Material.UpdateVertexShader(ShaderEntry.FileInfo.FullName);
        Material.UpdatePixelShader(ShaderEntry.FileInfo.FullName);

        Material.MaterialBuffer?.Dispose();

        MaterialCompiler.SetMaterialBuffer(this, new MaterialBuffer() { ShaderName = shaderName });
        Serialization.SaveXml(Material.MaterialBuffer, FileInfo.FullName);
    }
}

public sealed class MaterialCollector
{
    public List<MaterialEntry> Materials = new();

    public MaterialEntry GetMaterial(string name) =>
        Materials.Find(MaterialEntry => MaterialEntry.FileInfo.Name == name);
}

public class MaterialCompiler
{
    public static MaterialCollector MaterialCollector = new();

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

        MaterialEntry materialEntry = MaterialCollector.GetMaterial(fileInfo.Name);
        if (materialEntry is not null)
        {
            if (fileInfo.LastWriteTime == materialEntry.FileInfo.LastWriteTime)
                return;

            materialEntry.FileInfo = fileInfo;

            var materialBuffer = (MaterialBuffer)Serialization.LoadXml(typeof(MaterialBuffer), path);
            materialBuffer.PasteToPropertiesConstantBuffer();
            materialBuffer.UpdateConstantBuffer();

            Output.Log("Updated Material");
        }
        else
        {
            var materialBuffer = (MaterialBuffer)Serialization.LoadXml(typeof(MaterialBuffer), path);

            if (string.IsNullOrEmpty(materialBuffer.ShaderName))
                return;

            materialEntry = new MaterialEntry(fileInfo);
            SetMaterialBuffer(materialEntry, materialBuffer);

            materialBuffer.PasteToPropertiesConstantBuffer();

            MaterialCollector.Materials.Add(materialEntry);

            Output.Log("Read new Material");
        }
    }

    public static void SetMaterialBuffer(MaterialEntry materialEntry, MaterialBuffer materialBuffer)
    {
        var shaderEntry = ShaderCompiler.ShaderCollector.GetShader(materialBuffer.ShaderName);

        materialEntry.ShaderEntry = shaderEntry;
        materialEntry.Material = new(shaderEntry.FileInfo.FullName);
        materialEntry.Material.MaterialBuffer = materialBuffer;

        if(shaderEntry.ConstantBufferType is null)
        {
            Output.Log(
                "Could not create the MaterialBuffer PropertiesConstantBuffer, " +
                "because the ShaderEntry ConstantBufferType is null");
            return;
        }

        materialBuffer.CreateInstance(shaderEntry.ConstantBufferType);
        materialBuffer.CreateConstantBuffer(materialEntry.ShaderEntry.ConstantBufferType);
    }
}
