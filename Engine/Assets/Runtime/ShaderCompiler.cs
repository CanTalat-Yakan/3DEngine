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

public sealed class ShaderCompiler
{
    public CommonContext Context => _context ??= Kernel.Instance.Context;
    public CommonContext _context;

    public void CompileProjectShaders(string assetsPath = null)
    {
        if (assetsPath is null)
            return;

        string shadersFolderPath = Path.Combine(assetsPath, "Shaders");
        if (!Directory.Exists(shadersFolderPath))
            return;

        CheckShaderEntry(AssetPaths.SHADERS + "SimpleLit.hlsl");

        foreach (var shaderFilePath in Directory.GetFiles(shadersFolderPath, "*", SearchOption.AllDirectories))
            CheckShaderEntry(shaderFilePath);
    }

    private void CheckShaderEntry(string path)
    {
        FileInfo fileInfo = new(path);

        if (fileInfo.Extension != ".hlsl")
            return;

        if (!Assets.Shaders.TryGetValue(fileInfo.Name.RemoveExtension(), out var shaderEntry))
        {
            shaderEntry = new ShaderEntry()
            {
                FileInfo = fileInfo,
                ConstantBufferType = CreateMaterialPropertyBufferStruct(fileInfo.FullName)
            };
            Assets.Shaders.Add(fileInfo.Name.RemoveExtension(), shaderEntry);

            var shader = fileInfo.Name.RemoveExtension();
            Assets.VertexShaders[shader] = Context.GraphicsContext.LoadShader(DxcShaderStage.Vertex, AssetPaths.SHADERS + shader + ".hlsl", "VS");
            Assets.PixelShaders[shader] = Context.GraphicsContext.LoadShader(DxcShaderStage.Pixel, AssetPaths.SHADERS + shader + ".hlsl", "PS");
            Assets.PipelineStateObjects[shader] = new PipelineStateObject(Assets.VertexShaders[shader], Assets.PixelShaders[shader]);

            Output.Log("Read new Shader");
        }
        else if (fileInfo.LastWriteTime > shaderEntry.FileInfo.LastWriteTime)
        {
            shaderEntry.FileInfo = fileInfo;
            shaderEntry.ConstantBufferType = CreateMaterialPropertyBufferStruct(fileInfo.FullName);

            // Update already existing materials with the latest shader bytecode and properties constantbuffer.
            foreach (var materialEntry in Assets.Materials.Values)
                if (materialEntry.ShaderEntry.FileInfo == fileInfo)
                {
                    Kernel.Instance.Context.CreateShader(shaderEntry.FileInfo.FullName);

                    materialEntry.OnShaderUpdate?.Invoke();
                }

            var shader = fileInfo.Name.RemoveExtension();
            Assets.VertexShaders[shader] = Context.GraphicsContext.LoadShader(DxcShaderStage.Vertex, AssetPaths.SHADERS + shader + ".hlsl", "VS");
            Assets.PixelShaders[shader] = Context.GraphicsContext.LoadShader(DxcShaderStage.Pixel, AssetPaths.SHADERS + shader + ".hlsl", "PS");
            Assets.PipelineStateObjects[shader] = new PipelineStateObject(Assets.VertexShaders[shader], Assets.PixelShaders[shader]);

            Output.Log("Updated Shader");
        }
    }

    private Type CreateMaterialPropertyBufferStruct(string shaderFilePath)
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

        Kernel.Instance.ScriptCompiler.CompileScript(propertiesConstantBufferScriptEntry);

        if (propertiesConstantBufferScriptEntry.Assembly is null)
        {
            Output.Log("Compilation Failed", MessageType.Error);
            return null;
        }

        foreach (var type in propertiesConstantBufferScriptEntry.Assembly.GetTypes())
            if (type.ToString().Contains("Properties"))
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
            using System.Numerics;

            using Engine.Editor;

            public struct Properties{guid:N}

            """);

        // Start the scope.
        structBuilder.AppendLine("{");

        // Find all fields of the constant buffer Properties and loop through them.
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
                            processedField += $"    [{currentComment.TrimStart()}]\r\n";

                        currentComment = null; // Reset the comment variable.

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
