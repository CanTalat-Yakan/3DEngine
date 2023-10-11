using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System;

namespace Editor.Controller;

internal static partial class ExtensionMethods
{
    public static float Remap(this float value, float sourceMin, float sourceMax, float targetMin, float targetMax) =>
        (value - sourceMin) / (sourceMax - sourceMin) * (targetMax - targetMin) + targetMin;

    public static float Round(this float value, int digits = 2) =>
        MathF.Round(value, digits);

    public static Vector2 Round(this Vector2 value, int digits = 2) =>
        new Vector2(
            MathF.Round(value.X, digits),
            MathF.Round(value.Y, digits));

    public static Vector3 Round(this Vector3 value, int digits = 2) =>
        new Vector3(
            MathF.Round(value.X, digits),
            MathF.Round(value.Y, digits),
            MathF.Round(value.Z, digits));

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

    public static string SplitLast(this string text, char seperator) =>
        text.Split(seperator).Last();

    public static string FirstCharToUpper(this string input) =>
        string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1));

    public static string FormatString(this string text) =>
        text.SplitLast('_').SplitLast('.').SplitLast('+').FirstCharToUpper().AddSpacesToSentence();

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
            FileInfo file = new FileInfo(path);
            using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                stream.Close();
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
}
