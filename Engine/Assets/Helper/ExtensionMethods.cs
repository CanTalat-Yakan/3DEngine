using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SharpGen.Runtime;
using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Engine.Helper;

public static class ExtensionMethods
{
    public static SizeI Scale(this SizeI size, double scale) =>
        new SizeI((int)(size.Width * scale), (int)(size.Height * scale));

    public static float ToDegrees(this float value) =>
        MathHelper.ToDegrees(value);

    public static float ToRadians(this float value) =>
        MathHelper.ToRadians(value);

    public static Vector3 ToDegrees(this Vector3 vector) =>
        vector.SetVector(vector.X.ToDegrees(), vector.Y.ToDegrees(), vector.Z.ToDegrees());

    public static Vector3 ToRadians(this Vector3 vector) =>
        vector.SetVector(vector.X.ToRadians(), vector.Y.ToRadians(), vector.Z.ToRadians());

    public static Vector2 ToVector2(this SizeI size) =>
        new Vector2(size.Width, size.Height);

    public static Vector2 SetVector(this Vector2 vector, float x, float y)
    {
        vector.X = x; vector.Y = y;
        return vector;
    }

    public static Vector3 SetVector(this Vector3 vector, float x, float y, float z)
    {
        vector.X = x; vector.Y = y; vector.Z = z;
        return vector;
    }

    public static Dictionary<string, VertexBuffer> SetVertexBuffer(this Dictionary<string, VertexBuffer> vertices, ID3D12Resource resource, int sizeInByte)
    {
        foreach (var vertex in vertices.Values)
            vertex.SetVertexBuffer(resource, sizeInByte);

        return vertices;
    }

    public static VertexBuffer SetVertexBuffer(this VertexBuffer vertexBuffer, ID3D12Resource resource, int sizeInByte)
    {
        vertexBuffer.Resource = resource;
        vertexBuffer.SizeInByte = sizeInByte;

        return vertexBuffer;
    }

    public static List<float> ToFloats(this List<Vertex> vertices) =>
        vertices.SelectMany(
            vertex => vertex.position.ToFloats()
            .Concat(vertex.normal.ToFloats())
            .Concat(vertex.tangent.ToFloats())
            .Concat(vertex.uv.ToFloats()))
        .ToList();

    private static IEnumerable<float> ToFloats(this Vector3 vector)
    {
        yield return vector.X;
        yield return vector.Y;
        yield return vector.Z;
    }

    private static IEnumerable<float> ToFloats(this Vector2 vector)
    {
        yield return vector.X;
        yield return vector.Y;
    }
    public static bool IsNaN(this float value) =>
    float.IsNaN(value);
    public static bool IsNaN(this Vector2 vector) =>
        float.IsNaN(vector.X) || float.IsNaN(vector.Y);

    public static bool IsNaN(this Vector3 vector) =>
        float.IsNaN(vector.X) || float.IsNaN(vector.Y) || float.IsNaN(vector.Z);

    public static string SplitFirst(this string text, params char[] separators) =>
        text.Split(separators).FirstOrDefault();

    public static string SplitLast(this string text, params char[] separators) =>
        text.Split(separators).Last();

    public static string FirstCharToUpper(this string input) =>
        string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1));

    public static string GetFileName(this string text)
    {
        var splits = Path.GetFileName(text).Split('.');

        return splits[0] + "." + splits[1];
    }

    public static string RemoveExtension(this string text) =>
        text.Split('.').FirstOrDefault();

    public static string FormatString(this string text) =>
        text.SplitLast('_').SplitLast('.').SplitLast('+').FirstCharToUpper().AddSpacesToSentence();

    public static string AddSpacesToSentence(this string text, bool preserveAcronyms = true)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        StringBuilder newText = new StringBuilder(text.Length * 2);
        newText.Append(text[0]);
        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]) || char.IsDigit(text[i]))
                if (!char.IsDigit(text[i - 1]))
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                    (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                     i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
            newText.Append(text[i]);
        }

        return newText.ToString();
    }

    public static string IncrementNameIfExists(this string name, string[] list)
    {
        var i = 0;
        bool nameWithoutIncrement = list.Contains(name);

        foreach (var s in list)
            if (s == name || s.Contains(name + " ("))
                i++;

        if (i > 0 && nameWithoutIncrement)
            name += " (" + (i + 1).ToString() + ")";

        return name;
    }

    public static string IncrementPathIfExists(this string path, string[] list)
    {
        var name = Path.GetFileNameWithoutExtension(path);

        name = name.IncrementNameIfExists(list);

        return Path.Combine(Path.GetDirectoryName(path), name + Path.GetExtension(path));
    }

    public static bool? IsFileLocked(this string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            FileInfo fileInfo = new FileInfo(path);
            using (FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                fileStream.Close();
        }
        catch (IOException)
        {
            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            return true;
        }

        //file is not locked
        return false;
    }

    public static void ThrowIfFailed(this Result result)
    {
        if (result.Failure)
            throw new NotImplementedException(result.ToString());
    }
}