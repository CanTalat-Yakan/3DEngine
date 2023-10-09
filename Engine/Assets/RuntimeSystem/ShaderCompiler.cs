using System.Collections.Generic;
using System.IO;
using System.Text;

using Vortice.Direct3D11.Shader;
using Vortice.Direct3D;

namespace Engine.RuntimeSystem;

public sealed class ShaderEntry
{
    public FileInfo FileInfo;
    public Type ConstantBufferType;
}

public sealed class ShaderCollector
{
    public List<ShaderEntry> Shaders = new();

    public ShaderEntry GetShader(string name) =>
        Shaders.Find(ShaderEntry => ShaderEntry.FileInfo.Name == name);
}

public sealed class ShaderCompiler
{
    public static ShaderCollector ShaderCollector = new();

    private List<MaterialEntry> _toDisposeMaterialEntries = new();

    public void CompileProjectShaders(string assetsPath = null)
    {
        if (assetsPath is null)
            return;

        string shadersFolderPath = Path.Combine(assetsPath, "Shaders");
        if (!Directory.Exists(shadersFolderPath))
            return;

        ShaderCollector.Shaders.Clear();
        ShaderCollector.Shaders.Add(new()
        {
            FileInfo = new FileInfo(Path.Combine(Paths.SHADERS, "SimpleLit.hlsl")),
            ConstantBufferType = CreateMaterialBufferScript(AddToDispose(
                new MaterialEntry(null)
                {
                    Material = new(Path.Combine(Paths.SHADERS, "SimpleLit.hlsl"))
                }))
        });

        foreach (var path in Directory.GetFiles(shadersFolderPath, "*", SearchOption.AllDirectories))
            ShaderCollector.Shaders.Add(new()
            {
                FileInfo = new(path),
                ConstantBufferType = CreateMaterialBufferScript(AddToDispose(
                    new MaterialEntry(null)
                    {
                        Material = new(path)
                    }))
            });

        Dispose();
    }

    public void Dispose()
    {
        foreach (var materialEntry in _toDisposeMaterialEntries)
            materialEntry.Material?.Dispose();
    }

    private MaterialEntry AddToDispose(MaterialEntry materialEntry)
    {
        _toDisposeMaterialEntries.Add(materialEntry);

        return materialEntry;
    }

    private Type CreateMaterialBufferScript(MaterialEntry materialEntry)
    {
        string scriptCode = GenerateCSharpStruct(new(materialEntry.Material.NativePtr), Guid.NewGuid(), out string structName);

        ScriptEntry materialConstantBuffer = new();
        materialConstantBuffer.Script = ScriptCompiler.CreateScript(scriptCode);

        if (materialConstantBuffer.Script is null)
            return null; // Script Creation Failed

        Core.Instance.ScriptCompiler.CompileScript(materialConstantBuffer);

        if (materialConstantBuffer.Assembly is null)
            return null; // Compilation Failed

        foreach (var type in materialConstantBuffer.Assembly.GetTypes())
            if (typeof(IMaterialBuffer).IsAssignableFrom(type))
                return type; // Successfull

        return null; // A Script with the Interface IMaterialBuffer was not found
    }

    public static string GenerateCSharpStruct(ID3D11ShaderReflection shaderReflection, Guid guid, out string structName)
    {
        structName = null;

        ID3D11ShaderReflectionConstantBuffer properties = shaderReflection.GetConstantBufferByName("Properties");
        if (properties is null)
            return null;

        structName = $"{properties.Description.Name}{guid:N}";

        // Start building the C# struct definition.
        StringBuilder structBuilder = new StringBuilder();
        structBuilder.AppendLine($"struct {structName} : IMaterialBuffer {{");

        foreach (var variable in properties.Variables)
        {
            // Map the HLSL types to C# types.
            string csDataType = MapHLSLToCSharpType(variable.VariableType);
            structBuilder.AppendLine($"    public {csDataType} {variable.Description.Name};");
        }

        // Close the struct definition.
        structBuilder.AppendLine("}");

        return structBuilder.ToString();
    }

    private static string MapHLSLToCSharpType(ID3D11ShaderReflectionType typeDescription) =>
        typeDescription.Description.Type switch
        {
            ShaderVariableType.Int => "int",
            ShaderVariableType.Float => typeDescription.Description.Class switch
            {
                ShaderVariableClass.Scalar => "float",
                ShaderVariableClass.Vector => typeDescription.Description.RowCount switch
                {
                    1 => "float",
                    2 => "Vector2",
                    3 => "Vector3",
                    4 => "Vector4",
                    _ => "float"
                },
                _ => "float"
            },
            ShaderVariableType.Bool => "bool",
            _ => null
        };
}
