

namespace Engine
{
    public static class T4AssetPaths
    {
        public static readonly string DIRECTORY = @"E:\\3DEngine\\Engine\\Assets\\";
        public static readonly string RESOURCES = @"E:\\3DEngine\\Engine\\Assets\\Resources\\";
        public static readonly string SHADERS = @"E:\\3DEngine\\Engine\\Assets\\Resources\\Shaders\\";
        public static readonly string COMPUTE = @"E:\\3DEngine\\Engine\\Assets\\Resources\\ComputeShaders\\";
    }

    public enum ShaderFiles
    {
        ImGui,
        SimpleLit,
        Sky,
        Unlit,
    }

    public static class ShaderFileHelper
    {
        public static string SanitizeEnumMember(string name)
        {
            var sb = new StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_'); // Replace invalid characters with underscore
            }
            // Ensure the name doesn't start with a digit
            if (sb.Length > 0 && char.IsDigit(sb[0]))
                sb.Insert(0, '_');

            return sb.ToString();
        }
    }
}

