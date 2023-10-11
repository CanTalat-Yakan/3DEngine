using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

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

        foreach (var shaderFilePath in Directory.GetFiles(shadersFolderPath, "*", SearchOption.AllDirectories))
            ShaderCollector.Shaders.Add(new()
            {
                FileInfo = new(shaderFilePath),
                ConstantBufferType = CreateMaterialBufferScript(new(shaderFilePath))
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

        structBuilder.Append(
            $"""
            using System.Drawing;
            using System.Numerics;

            using Engine.Editor;
            using Engine.Rendering;

            public struct Properties{guid:N} : IMaterialBuffer

            """);

        // Start the scope.
        structBuilder.AppendLine("{");

        // TODO: Find all Fields of the cbuffer Properties and Loop through them
        var propertiesBufferFields = ProcessConstantBufferFields(shaderCode);
        foreach (var field in propertiesBufferFields)
            structBuilder.AppendLine(field);

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

            string currentComment = null; // Initialize a variable to store the current comment.

            foreach (string line in lines)
                // Check for comments and store them.
                if (line.Trim().StartsWith("//"))
                    currentComment = line.Trim().TrimStart('/');
                else
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

                        string processedField = string.Empty;

                        // Combine the comment (if any) with the processed field string.
                        if (!string.IsNullOrEmpty(currentComment))
                        {
                            currentComment = currentComment.TrimStart();

                            if (currentComment.Contains("Color"))
                            {
                                if (hlslFieldType.Equals("Vector4"))
                                    hlslFieldType = "Color";
                            }
                            else
                                processedField += $"    [{currentComment}]\r\n";

                            currentComment = null; // Reset the comment variable.
                        }

                        processedField += $"    public {hlslFieldType} {hlslFieldName};";

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
            "float4" => "Vector4",
            "bool" => "bool",
            _ => null
        };
}
