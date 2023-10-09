namespace Engine;

public sealed class Paths
{
    public static readonly string APPDIRECTORY = AppContext.BaseDirectory;

    public static readonly string ASSETS = APPDIRECTORY + @"\Assets\";

    public static readonly string RESOURCES =   ASSETS + @"Resources\";

    public static readonly string TEMPLATES =   RESOURCES + @"Templates\";
    public static readonly string SHADERS =     RESOURCES + @"Shaders\";
    public static readonly string TEXTURES =    RESOURCES + @"Textures\";
    public static readonly string MODELS =      RESOURCES + @"Models\";
}
