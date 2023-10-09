using System.Collections.Generic;
using System.IO;
using System.Text;

using Vortice.D3DCompiler;
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
            FileInfo = new FileInfo(Paths.SHADERS + "SimpleLit.hlsl"),
            ConstantBufferType = CreateMaterialBufferScript(new(Paths.SHADERS + "SimpleLit.hlsl"))
        });

        foreach (var shaderFIlePath in Directory.GetFiles(shadersFolderPath, "*", SearchOption.AllDirectories))
            ShaderCollector.Shaders.Add(new()
            {
                FileInfo = new(shaderFIlePath),
                ConstantBufferType = CreateMaterialBufferScript(new(shaderFIlePath))
            });
    }

    private Type CreateMaterialBufferScript(string shaderFilePath)
    {
        if (!File.Exists(shaderFilePath))
            return null;

        var readOnlyMemory = Material.CompileBytecode(shaderFilePath, "VS", "vs_5_0");
        string scriptCode = GenerateCSharpStruct(readOnlyMemory.Span, Guid.NewGuid());

        if (scriptCode is null)
        {
            Output.Log("Generating the C# Struct from the Shader Failed", MessageType.Error);
            return null;
        }

        ScriptEntry materialConstantBufferScriptEntry = new();
        materialConstantBufferScriptEntry.Script = ScriptCompiler.CreateScript(scriptCode);

        if (materialConstantBufferScriptEntry.Script is null)
        {
            Output.Log("Script Creation Failed", MessageType.Error);
            return null;
        }

        Core.Instance.ScriptCompiler.CompileScript(materialConstantBufferScriptEntry);

        if (materialConstantBufferScriptEntry.Assembly is null)
        {
            Output.Log("Compilation Failed", MessageType.Error);
            return null;
        }

        foreach (var type in materialConstantBufferScriptEntry.Assembly.GetTypes())
            if (typeof(IMaterialBuffer).IsAssignableFrom(type))
                return type; // Successfull.

        Output.Log("A Script with the Interface IMaterialBuffer was not found", MessageType.Error);
        return null;
    }

    public static string GenerateCSharpStruct(ReadOnlySpan<byte> shaderByteCode, Guid guid)
    {
        var shaderReflection = Compiler.Reflect<ID3D11ShaderReflection>(shaderByteCode);

        ID3D11ShaderReflectionConstantBuffer properties = null;

        foreach (var constantBuffer in shaderReflection.ConstantBuffers)
            if (constantBuffer.Description.Name.Equals("Properties"))
                properties = constantBuffer;

        if (properties is null)
            return null;

        // Start building the C# struct definition.
        StringBuilder structBuilder = new StringBuilder();
        structBuilder.AppendLine($"struct {properties.Description.Name}{guid:N} : IMaterialBuffer {{");

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
