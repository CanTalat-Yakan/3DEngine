using System.Collections.Generic;
using System.IO;

namespace Engine.Utilities;

public sealed class MaterialEntry
{
    public Guid ID = Guid.NewGuid();

    public FileInfo FileInfo;
    public Material Material;
    public string Name => FileInfo.Name;
}

public sealed class MaterialCollector
{
    public List<MaterialEntry> Materials = new();

    public Material GetMaterial(string name) =>
        Materials.Find(Material => Material.Name == name).Material;
}

public class MaterialCompiler
{
    public MaterialCollector MaterialCollector = new();

    private Dictionary<string, MaterialEntry> _materialCollection = new();

    public void CompileProjectMaterials(string assetsPath = null)
    {
        return;
        if (assetsPath is null)
            return;

        string shadersFolderPath = Path.Combine(assetsPath, "Shaders");
        if (!Directory.Exists(shadersFolderPath))
            return;

        foreach (var path in Directory.GetFiles(shadersFolderPath, "*", SearchOption.AllDirectories))
            CheckMaterialEntry(path);
    }

    private MaterialEntry CheckMaterialEntry(string path)
    {
        FileInfo fileInfo = new(path);

        if (_materialCollection.TryGetValue(fileInfo.FullName, out MaterialEntry materialEntry))
        {
            if (fileInfo.LastWriteTime > materialEntry.FileInfo.LastWriteTime)
            {
                materialEntry.FileInfo = fileInfo;

                materialEntry.Material.UpdateVertexShader(path);
                materialEntry.Material.UpdatePixelShader(path);

                UpdateMaterialBuffer(materialEntry, path);

                Output.Log("Updated Material");
            }
            else
                materialEntry = null;
        }
        else
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                materialEntry = new() { FileInfo = fileInfo };
                materialEntry.Material = new(path, null);

                _materialCollection.Add(fileInfo.FullName, materialEntry);

                UpdateMaterialBuffer(materialEntry, path);

                Output.Log("Read new Material");
            }
        }

        return materialEntry;
    }

    private void UpdateMaterialBuffer(MaterialEntry materialEntry, string path)
    {
        string updatedCode = File.ReadAllText(path);
        string scriptCode = MaterialBuffer.ConvertHlslToCSharp(updatedCode, materialEntry.ID);

        ScriptEntry materialBuffer = new();
        materialBuffer.Script = RuntimeCompiler.CreateScript(scriptCode);

        if (materialBuffer.Script is null)
            return;

        Core.Instance.RuntimeCompiler.CompileScript(materialBuffer);

        if (materialBuffer.Assembly is null)
            return;

        foreach (var type in materialBuffer.Assembly.GetTypes())
            if (typeof(IMaterialBuffer).IsAssignableFrom(type))
            {

            }
    }
}
