using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace Engine.Runtime;

public sealed class ShaderEntry
{
    public FileInfo FileInfo;
    public Type ConstantBufferType;
}

public sealed class ShaderLibrary
{
    public List<ShaderEntry> Shaders = new();

    public ShaderEntry GetShader(string shaderName) =>
        Shaders.Find(ShaderEntry => Equals(
            ShaderEntry.FileInfo.Name.RemoveExtension(),
            shaderName.RemoveExtension()));
}

public sealed class ShaderCompiler
{
    public static ShaderLibrary ShaderLibrary = new();

    public void CompileProjectShaders(string assetsPath = null)
    {
        if (assetsPath is null)
            return;

        string shadersFolderPath = Path.Combine(assetsPath, "Shaders");
        if (!Directory.Exists(shadersFolderPath))
            return;

        CheckShaderEntry(Paths.SHADERS + "SimpleLit.hlsl");

        foreach (var shaderFilePath in Directory.GetFiles(shadersFolderPath, "*", SearchOption.AllDirectories))
            CheckShaderEntry(shaderFilePath);
    }

    private void CheckShaderEntry(string path)
    {
        FileInfo fileInfo = new(path);

        var shaderEntry = ShaderLibrary.GetShader(fileInfo.Name);
        if (shaderEntry is null)
        {
            ShaderLibrary.Shaders.Add(new()
            {
                FileInfo = fileInfo,
                ConstantBufferType = CreateMaterialBufferScript(fileInfo.FullName)
            });

            Output.Log("Read new Shader");
        }
        else if (fileInfo.LastWriteTime > shaderEntry.FileInfo.LastWriteTime)
        {
            shaderEntry.FileInfo = fileInfo;
            shaderEntry.ConstantBufferType = CreateMaterialBufferScript(fileInfo.FullName);

            Output.Log("Updated Shader");
        }
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

        ScriptEntry propertiesConstantBufferScriptEntry = new() { FileInfo = new(shaderFilePath) };
        propertiesConstantBufferScriptEntry.Script = ScriptCompiler.CreateScript(scriptCode);

        if (propertiesConstantBufferScriptEntry.Script is null)
        {
            Output.Log("Script Creation Failed", MessageType.Error);
            return null;
        }

        Core.Instance.ScriptCompiler.CompileScript(propertiesConstantBufferScriptEntry);

        if (propertiesConstantBufferScriptEntry.Assembly is null)
        {
            Output.Log("Compilation Failed", MessageType.Error);
            return null;
        }

        foreach (var type in propertiesConstantBufferScriptEntry.Assembly.GetTypes())
            if (typeof(IMaterialBuffer).IsAssignableFrom(type))
                return type; // Successful.

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
