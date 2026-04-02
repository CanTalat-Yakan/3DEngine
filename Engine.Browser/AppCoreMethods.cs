using System.Runtime.InteropServices;
using UltralightNet;

namespace Engine;

/// <summary>
/// P/Invoke wrappers for the Ultralight AppCore library (SDK 1.3.0).
/// These register native platform implementations (font loader, file system, logger)
/// and set the corresponding managed <see cref="ULPlatform"/> flags so that
/// <see cref="ULPlatform.CreateRenderer()"/> skips its managed validation.
/// </summary>
internal static unsafe class AppCoreMethods
{
    [DllImport("AppCore", CallingConvention = CallingConvention.Cdecl)]
    private static extern void ulEnablePlatformFontLoader();

    [DllImport("AppCore", EntryPoint = "ulEnablePlatformFileSystem", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ulEnablePlatformFileSystemNative(ULString* baseDirectory);

    [DllImport("AppCore", EntryPoint = "ulEnableDefaultLogger", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ulEnableDefaultLoggerNative(ULString* logPath);

    public static void SetPlatformFontLoader()
    {
        ulEnablePlatformFontLoader();
        ULPlatform.SetDefaultFontLoader = false;
    }

    public static void SetPlatformFileSystem(string baseDirectory)
    {
        using var str = new ULString(baseDirectory.AsSpan());
        ulEnablePlatformFileSystemNative(&str);

        ULPlatform.SetDefaultFileSystem = false;
        ULPlatform.ErrorMissingResources = false;
    }

    public static void SetDefaultLogger(string logPath)
    {
        using var str = new ULString(logPath.AsSpan());
        ulEnableDefaultLoggerNative(&str);

        ULPlatform.EnableDefaultLogger = false;
    }
}

