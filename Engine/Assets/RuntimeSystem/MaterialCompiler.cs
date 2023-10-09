using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Engine.RuntimeSystem;

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

        Material.UpdateVertexShader(ShaderEntry.FileInfo.Name);
        Material.UpdatePixelShader(ShaderEntry.FileInfo.Name);

        Material.MaterialBuffer?.Dispose();
        MaterialCompiler.SetMaterialBuffer(this, new MaterialBuffer(shaderName));
        Serialization.SaveXml(Material.MaterialBuffer, FileInfo.FullName);
    }
}

public sealed class MaterialCollector
{
    public List<MaterialEntry> Materials = new();

    public Material GetMaterial(string name) =>
        Materials.Find(Material => Material.FileInfo.Name == name).Material;
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

    private MaterialEntry CheckMaterialEntry(string path)
    {
        FileInfo fileInfo = new(path);

        MaterialEntry materialEntry = MaterialCollector.Materials.FirstOrDefault(entry => entry.FileInfo == fileInfo);
        if (materialEntry is not null)
        {
            if (fileInfo.LastWriteTime > materialEntry.FileInfo.LastWriteTime)
            {
                materialEntry.FileInfo = fileInfo;

                var materialBuffer = (MaterialBuffer)Serialization.LoadXml(typeof(MaterialBuffer), path);
                materialBuffer.UpdateConstantBuffer();

                Output.Log("Updated Material");
            }
            else
                materialEntry = null;
        }
        else
        {
            var materialBuffer = (MaterialBuffer)Serialization.LoadXml(typeof(MaterialBuffer), path);

            SetMaterialBuffer(new MaterialEntry(fileInfo), materialBuffer);

            Output.Log("Read new Material");
        }

        return materialEntry;
    }

    public static void SetMaterialBuffer(MaterialEntry materialEntry, MaterialBuffer materialBuffer)
    {
        var shaderEntry = ShaderCompiler.ShaderCollector.GetShader(materialBuffer.ShaderName);

        materialEntry.ShaderEntry = shaderEntry;
        materialEntry.Material = new(shaderEntry.FileInfo.FullName);
        materialEntry.Material.MaterialBuffer = materialBuffer;

        materialBuffer.PropertiesConstantBuffer = Activator.CreateInstance(shaderEntry.ConstantBufferType);
        materialBuffer.CreateConstantBuffer(materialEntry.ShaderEntry.ConstantBufferType);
    }
}
