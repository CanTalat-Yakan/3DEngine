using System.Collections.Generic;
using System.IO;

namespace Engine.Runtime;

public sealed class MaterialEntry(FileInfo fileInfo)
{
    public FileInfo FileInfo = fileInfo;
    public Material_OLD Material;

    public ShaderEntry ShaderEntry;
    public Action OnShaderUpdate;

    public void SetShader(ShaderEntry shaderEntry)
    {
        if (shaderEntry is null)
            return;

        ShaderEntry = shaderEntry;

        Material.UpdateShader(shaderEntry.FileInfo.FullName);

        Material.MaterialBuffer?.Dispose();

        MaterialCompiler.SetMaterialBuffer(this, new MaterialBuffer() { ShaderName = shaderEntry.FileInfo.Name.RemoveExtension() });
        Serialization.SaveFile(Material.MaterialBuffer, FileInfo.FullName);
    }
}

public sealed class MaterialLibrary
{
    public List<MaterialEntry> Materials = new();

    public MaterialEntry GetMaterial(string materialName) =>
        Materials.Find(MaterialEntry => Equals(
            MaterialEntry.FileInfo.Name.RemoveExtension(),
            materialName.RemoveExtension()));
}

public class MaterialCompiler
{
    public static MaterialLibrary Library { get; private set; } = new();

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

        var materialEntry = Library.GetMaterial(fileInfo.Name);
        if (materialEntry is null)
        {
            var materialBuffer = Serialization.LoadFile<MaterialBuffer>(path);

            if (string.IsNullOrEmpty(materialBuffer.ShaderName))
                return;

            materialEntry = new MaterialEntry(fileInfo);
            SetMaterialBuffer(materialEntry, materialBuffer);

            materialBuffer.PasteToPropertiesConstantBuffer();

            Library.Materials.Add(materialEntry);

            Output.Log("Read new Material");
        }
        else if (fileInfo.LastWriteTime > materialEntry.FileInfo.LastWriteTime)
        {
            materialEntry.FileInfo = fileInfo;

            var materialBuffer = Serialization.LoadFile<MaterialBuffer>(path);

            SetMaterialBuffer(materialEntry, materialBuffer);

            materialBuffer.PasteToPropertiesConstantBuffer();
            materialBuffer.UpdatePropertiesConstantBuffer();

            Output.Log("Updated Material");
        }
    }

    public static void SetMaterialBuffer(MaterialEntry materialEntry, MaterialBuffer materialBuffer)
    {
        var shaderEntry = ShaderCompiler.Library.GetShader(materialBuffer.ShaderName);

        materialEntry.ShaderEntry = shaderEntry;
        materialEntry.Material = new(shaderEntry.FileInfo.FullName);
        materialEntry.Material.MaterialBuffer?.Dispose();
        materialEntry.Material.MaterialBuffer = materialBuffer;

        if (shaderEntry.ConstantBufferType is null)
        {
            Output.Log(
                "Could not create the MaterialBuffer PropertiesConstantBuffer, " +
                "because the ShaderEntry ConstantBufferType is null");
            return;
        }

        materialBuffer.CreatePerModelConstantBuffer();
        materialBuffer.CreatePropertiesConstantBuffer(shaderEntry.ConstantBufferType);
    }
}
