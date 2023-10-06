using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Engine.Utilities;

internal class MaterialBuffer
{
    internal static string ProcessConstantBufferToScriptComponent(string code)
    {
        string processedCode = code;

        return processedCode;
    }

    public static string ConvertHlslToCSharp(string hlslCode, Guid bufferGuid)
    {
        // Define a regular expression pattern to match constant buffer declarations
        string pattern = @"cbuffer\s+(\w+)\s*:\s*register\((b\d+)\)(.*?)\}";

        // Find all matches for constant buffer declarations in the HLSL code
        MatchCollection matches = Regex.Matches(hlslCode, pattern, RegexOptions.Singleline);

        // Generate C# code for each constant buffer declaration
        List<string> csharpCodeList = new List<string>();

        foreach (Match match in matches)
        {
            string bufferName = match.Groups[1].Value;
            string slotNumber = match.Groups[2].Value;
            string bufferFields = match.Groups[3].Value.Trim();

            // Generate the C# class code for the constant buffer
            string csharpCode = GenerateCSharpClass(bufferName, bufferGuid, slotNumber, bufferFields);
            csharpCodeList.Add(csharpCode);
        }

        // Combine all C# class definitions into a single string
        string csharpCodeResult = string.Join(Environment.NewLine, csharpCodeList);

        return csharpCodeResult;
    }

    private static string GenerateCSharpClass(string bufferName, Guid bufferGuid, string slotNumber, string bufferFields)
    {
        // Handle [Color] annotation for float3 fields
        bufferFields = Regex.Replace(bufferFields, @"\/\/\s*\[Color\]\s*(float3\s+\w+)", "Color $1");
        // Handle custom annotations like [Slider(min, max)] for float and int fields
        bufferFields = Regex.Replace(bufferFields, @"\/\/\s*\[Slider\(([^,]+),\s*([^)]+)\)\]\s*(float|int)\s+(\w+)",
            match =>
            {
                string min = match.Groups[1].Value.Trim();
                string max = match.Groups[2].Value.Trim();
                string fieldType = match.Groups[3].Value.Trim();
                string fieldName = match.Groups[4].Value.Trim();

                // Determine the corresponding C# type based on the HLSL type
                string csharpType = (fieldType == "float") ? "float" : "int";

                // Generate the field declaration with the Slider attribute
                return $"[Slider({min}, {max})]{csharpType} {fieldName};";
            });

        // Perform the conversion for float3 to Color and float2 to Vector2
        bufferFields = Regex.Replace(bufferFields, @"float3\s+(\w+)", "Color $1");
        bufferFields = Regex.Replace(bufferFields, @"float2\s+(\w+)", "Vector2 $1");


        // Create the C# class code
        string csharpCode = $@"
            using System.Drawing;
            
            namespace Engine
            {{
                public class {bufferName}{bufferGuid:N} : Component, IMaterialBuffer
                {{
                    public int Slot => {slotNumber};
            
                    {bufferFields}
                }}
            }}";

        return csharpCode;
    }

    private static string GenerateSimpleCSharpClass(string bufferName, Guid bufferGuid, string slotNumber, string bufferFields)
    {
        // Create the C# class code
        string csharpCode = $@"
            using System.Drawing;
            
            namespace Engine
            {{
                public class {bufferName}{bufferGuid:N} : Component, IMaterialBuffer
                {{
                    public int Slot => {slotNumber};
            
                    {bufferFields}
                }}
            }}";

        return csharpCode;
    }
}

internal interface IMaterialBuffer { }