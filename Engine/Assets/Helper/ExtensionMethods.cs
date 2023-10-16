using System.Drawing;
using System.Linq;
using System.Text;

using Vortice.Mathematics;

namespace Engine.Helper;

public static class ExtensionMethods
{
    public static Size Scale(this Size size, double scale) =>
        new Size((int)(size.Width * scale), (int)(size.Height * scale));

    public static float ToDegrees(this float value) =>
        MathHelper.ToDegrees(value);

    public static float ToRadians(this float value) =>
        MathHelper.ToRadians(value);

    public static Vector3 ToDegrees(this Vector3 vector) =>
        new(vector.X.ToDegrees(), vector.Y.ToDegrees(), vector.Z.ToDegrees());

    public static Vector3 ToRadians(this Vector3 vector) =>
        new(vector.X.ToRadians(), vector.Y.ToRadians(), vector.Z.ToRadians());

    public static Vector2 ToVector2(this Size size) =>
        new Vector2(size.Width, size.Height);

    public static bool IsNaN(this float value) =>
        float.IsNaN(value);

    public static bool IsNaN(this Vector2 vector) =>
        float.IsNaN(vector.X) || float.IsNaN(vector.Y);

    public static bool IsNaN(this Vector3 vector) =>
        float.IsNaN(vector.X) || float.IsNaN(vector.Y) || float.IsNaN(vector.Z);

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

    public static string SplitFirst(this string text, params char[] separators) =>
        text.Split(separators).FirstOrDefault();

    public static string SplitLast(this string text, params char[] separators) =>
        text.Split(separators).Last();

    public static string FirstCharToUpper(this string input) =>
        string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1));

    public static string FormatString(this string text) =>
        text.SplitLast('_').SplitLast('.').SplitLast('+').FirstCharToUpper().AddSpacesToSentence();

    public static string RemoveExtension(this string text) =>
        text.Split('.').FirstOrDefault();
}
