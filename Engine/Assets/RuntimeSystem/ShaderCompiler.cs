using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

using Vortice.D3DCompiler;
using Vortice.Direct3D11.Shader;
using System.Linq;

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
        Shaders.Find(ShaderEntry => Equals(
            ShaderEntry.FileInfo.Name.Split('.').FirstOrDefault(),
            name));
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

        string scriptCode = GenerateCSharpStructWithRegex(File.ReadAllText(shaderFilePath), Guid.NewGuid());

        if (scriptCode is null)
        {
            Output.Log("Generating the C# Struct from the Shader Failed", MessageType.Error);
            return null;
        }

        ScriptEntry materialConstantBufferScriptEntry = new() { FileInfo = new(shaderFilePath) };
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

    public static string GenerateCSharpStructWithRegex(string shaderCode, Guid guid)
    {
        // Start building the C# struct definition.
        StringBuilder structBuilder = new StringBuilder();
        structBuilder.AppendLine("using System.Numerics;");
        structBuilder.AppendLine("using Engine.Rendering;");

        structBuilder.AppendLine($"public struct Properties{guid:N} : IMaterialBuffer");
        structBuilder.AppendLine("{");

        // TODO: Find all Fields of the cbuffer Properties and Loop through them
        var propertiesBufferFields = ProcessConstantBufferFields(shaderCode);
        foreach (var field in propertiesBufferFields)
            structBuilder.AppendLine($"    public {field};");

        // Close the struct definition.
        structBuilder.AppendLine("}");

        return structBuilder.ToString();
    }

    public static string GenerateCSharpStructWithShaderReflection(ReadOnlySpan<byte> shaderByteCode, Guid guid)
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
        structBuilder.AppendLine("using System.Numerics;");
        structBuilder.AppendLine("using Engine.Rendering;");

        structBuilder.AppendLine($"struct {properties.Description.Name}{guid:N} : IMaterialBuffer");
        structBuilder.AppendLine("{");

        foreach (var variable in properties.Variables)
        {
            // Map the HLSL types to C# types.
            string csDataType = MapHLSLToCSharpType(variable.VariableType.Description.Type.ToString().ToLower());

            structBuilder.AppendLine($"    public {csDataType} {variable.Description.Name};");
        }

        // Close the struct definition.
        structBuilder.AppendLine("}");

        return structBuilder.ToString();
    }

    public static List<string> ProcessConstantBufferFields(string shaderCode)
    {
        List<string> processedFields = new List<string>();

        // Define a regular expression pattern to match constant buffer fields.
        string pattern = @"cbuffer\s+Properties\s*:\s*register\(b\d+\)\s*{([^}]+)};";
        Regex regex = new Regex(pattern, RegexOptions.Multiline);

        // Find the first match of the pattern in the shader code.
        Match match = regex.Match(shaderCode);

        if (match.Success)
        {
            string bufferContent = match.Groups[1].Value;
            string[] lines = bufferContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // Extract the field type and name using a regular expression.
                Match fieldMatch = Regex.Match(line, @"\s*(\w+)\s+(\w+);");
                if (fieldMatch.Success)
                {
                    string hlslFieldType = fieldMatch.Groups[1].Value;
                    string hlslFieldName = fieldMatch.Groups[2].Value;

                    hlslFieldType = MapHLSLToCSharpType(hlslFieldType);
                    if (string.IsNullOrEmpty(hlslFieldType))
                        continue;

                    // Add the processed field string (field type and name) to the list.
                    string processedField = $"{hlslFieldType} {hlslFieldName}";
                    processedFields.Add(processedField);
                }
            }
        }

        return processedFields;
    }

    private static string MapHLSLToCSharpType(string hlslType) =>
            hlslType switch
            {
                "int" => "int",
                "float" => "float",
                "float2" => "Vector2",
                "float3" => "Vector3",
                "bool" => "bool",
                _ => null
            };
}
