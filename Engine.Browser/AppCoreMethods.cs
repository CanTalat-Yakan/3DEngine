using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using ForkAppCore = UltralightNet.AppCore.AppCoreMethods;

namespace Engine;

/// <summary>
/// Thin wrapper around UltralightNet.AppCore.AppCoreMethods (from the local 1.4 fork).
/// Registers native platform implementations (font loader, file system, logger)
/// via the AppCore native library.
/// </summary>
internal static class AppCoreMethods
{
    private static readonly ILogger Logger = Log.Category("Engine.Browser");
    private static bool _nativeLibsLoaded;

    public static void SetPlatformFontLoader()
    {
        EnsureNativeLibsLoaded();
        ForkAppCore.SetPlatformFontLoader();
    }

    public static void SetPlatformFileSystem(string baseDirectory)
    {
        EnsureNativeLibsLoaded();
        ForkAppCore.ulEnablePlatformFileSystem(baseDirectory);
    }

    public static void SetDefaultLogger(string logPath)
    {
        EnsureNativeLibsLoaded();
        ForkAppCore.ulEnableDefaultLogger(logPath);
    }

    /// <summary>
    /// Pre-loads all Ultralight native libraries into the process before any
    /// P/Invoke call can trigger. This ensures dlopen finds them by name
    /// since they're already loaded in memory.
    /// </summary>
    private static void EnsureNativeLibsLoaded()
    {
        if (_nativeLibsLoaded) return;
        _nativeLibsLoaded = true;

        string baseDir = AppContext.BaseDirectory;
        string nativeDir = Path.Combine(baseDir, "runtimes", "linux-x64", "native");

        if (!Directory.Exists(nativeDir))
        {
            Logger.Warn($"Native directory not found: {nativeDir}");
            return;
        }

        // Ensure libbz2.so.1.0 compat symlink exists (Ultralight SDK was built
        // against libbz2.so.1.0 but modern distros ship libbz2.so.1 → libbz2.so.1.0.x)
        EnsureBz2Compat(nativeDir);

        // Load order matters: dependencies first
        string[] libs = [
            "libUltralightCore.so",
            "libWebCore.so",
            "libUltralight.so",
            "libAppCore.so"
        ];

        foreach (string lib in libs)
        {
            string fullPath = Path.Combine(nativeDir, lib);
            if (NativeLibrary.TryLoad(fullPath, out nint handle))
            {
                Logger.Debug($"Pre-loaded native library: {fullPath} (handle=0x{handle:X})");
            }
            else
            {
                Logger.Warn($"Failed to pre-load native library: {fullPath}");
            }
        }

        // Also register a DllImportResolver for the AppCore assembly
        // so P/Invoke name "AppCore" maps to the already-loaded libAppCore.so
        var appCoreAssembly = typeof(ForkAppCore).Assembly;
        NativeLibrary.SetDllImportResolver(appCoreAssembly, ResolveUltralightLibrary);

        // And a global last-resort handler
        AssemblyLoadContext.Default.ResolvingUnmanagedDll += (_, name) =>
            ResolveUltralightLibrary(name, appCoreAssembly, null);
    }

    /// <summary>
    /// Ensures a libbz2.so.1.0 compatibility symlink exists in the native directory.
    /// libWebCore.so was linked against libbz2.so.1.0, but modern distros only ship
    /// libbz2.so.1 (→ libbz2.so.1.0.x). Create a symlink so dlopen can find it.
    /// </summary>
    private static void EnsureBz2Compat(string nativeDir)
    {
        string target = Path.Combine(nativeDir, "libbz2.so.1.0");
        if (File.Exists(target))
            return;

        // Try to find a system libbz2.so.1 or libbz2.so.1.0.*
        string[] candidates = [
            "/usr/lib/x86_64-linux-gnu/libbz2.so.1",
            "/usr/lib64/libbz2.so.1",
            "/usr/lib/libbz2.so.1"
        ];

        foreach (string candidate in candidates)
        {
            if (!File.Exists(candidate)) continue;

            try
            {
                // Resolve through symlinks to get the real file
                string realPath = Path.GetFullPath(candidate);
                File.CreateSymbolicLink(target, realPath);
                Logger.Debug($"Created libbz2 compat symlink: {target} → {realPath}");
                return;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to create libbz2 compat symlink: {ex.Message}");
            }
        }

        Logger.Warn("libbz2.so.1.0 not found and could not create compatibility symlink");
    }

    private static nint ResolveUltralightLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        string? fileName = libraryName switch
        {
            "Ultralight"     => "libUltralight.so",
            "UltralightCore" => "libUltralightCore.so",
            "WebCore"        => "libWebCore.so",
            "AppCore"        => "libAppCore.so",
            _ => null
        };

        if (fileName is null)
            return nint.Zero;

        string baseDir = AppContext.BaseDirectory;
        string fullPath = Path.Combine(baseDir, "runtimes", "linux-x64", "native", fileName);
        if (NativeLibrary.TryLoad(fullPath, out nint handle))
            return handle;

        string flatPath = Path.Combine(baseDir, fileName);
        if (NativeLibrary.TryLoad(flatPath, out handle))
            return handle;

        return nint.Zero;
    }
}
