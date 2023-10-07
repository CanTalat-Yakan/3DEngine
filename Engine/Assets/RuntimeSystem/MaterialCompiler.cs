using System.Collections.Generic;
using System.IO;

namespace Engine.RuntimeSystem;

public sealed class MaterialEntry
{
    public Guid ID = Guid.NewGuid();

    public FileInfo FileInfo;
    public Material Material;

    public FileInfo ShaderFileInfo;

    public object Buffer;

    public string Name => FileInfo.Name;

    public void SetShader(string path)
    {
        Material.UpdateVertexShader(path);
        Material.UpdatePixelShader(path);

        // TODO: Update Material XML File
    }
}

public sealed class MaterialCollector
{
    public List<MaterialEntry> Materials = new();

    public Material GetMaterial(string name) =>
        Materials.Find(Material => Material.Name == name).Material;
}

public sealed class ShaderCollector
{
    public List<FileInfo> Shaders = new();

    public FileInfo GetShader(string name) =>
        Shaders.Find(FileInfo => FileInfo.Name == name);
}

public class MaterialCompiler
{
    public MaterialCollector MaterialCollector = new();
    public ShaderCollector ShaderCollector = new();

    private Dictionary<string, MaterialEntry> _materialCollection = new();

    public void CompileProjectMaterials(string assetsPath = null)
    {
        if (assetsPath is null)
            return;

        string shadersFolderPath = Path.Combine(assetsPath, "Shaders");
        if (!Directory.Exists(shadersFolderPath))
            return;

        foreach (var path in Directory.GetFiles(shadersFolderPath, "*", SearchOption.AllDirectories))
        {
            ShaderCollector.Shaders.Clear();
            ShaderCollector.Shaders.Add(new(path));
        }

        ShaderCollector.Shaders.Add(new FileInfo(Path.Combine(Paths.SHADERS, "SimpleLit.hlsl")));

        string materialsFolderPath = Path.Combine(assetsPath, "Materials");
        if (!Directory.Exists(materialsFolderPath))
            return;

        foreach (var path in Directory.GetFiles(materialsFolderPath, "*", SearchOption.AllDirectories))
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

                // TODO: Read the material XML File and paste those information into a the Buffer

                // materialEntry.Buffer;
                // Type PropertyConstantBufferType = CreateMaterialBufferScript(materialEntry, path);

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
                //materialEntry.Material = new(path, null);

                _materialCollection.Add(fileInfo.FullName, materialEntry);

                // TODO: Create the Buffer and paste the information of the XML File into that one

                Output.Log("Read new Material");
            }
        }

        return materialEntry;
    }

    private Type CreateMaterialBufferScript(MaterialEntry materialEntry, string path)
    {
        string updatedCode = File.ReadAllText(path);
        string scriptCode = MaterialBuffer.ConvertHlslToCSharp(updatedCode, materialEntry.ID);

        ScriptEntry materialBuffer = new();
        materialBuffer.Script = ScriptCompiler.CreateScript(scriptCode);

        if (materialBuffer.Script is null)
            return null; // Script Creation Failed

        Core.Instance.ScriptCompiler.CompileScript(materialBuffer);

        if (materialBuffer.Assembly is null)
            return null; // Compilation Failed

        foreach (var type in materialBuffer.Assembly.GetTypes())
            if (typeof(IMaterialBuffer).IsAssignableFrom(type))
                return type; // Successfull

        return null; // A Script with the Interface IMaterialBuffer was not found
    }
}
