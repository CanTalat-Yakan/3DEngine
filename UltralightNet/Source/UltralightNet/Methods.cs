using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("UltralightNet.AppCore")]
#if NET7_0_OR_GREATER
[assembly: DisableRuntimeMarshalling]
#endif

#if RELEASE
[module: SkipLocalsInit]
#endif

namespace UltralightNet;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "<Pending>")]
public static unsafe partial class Methods
{
	public const string LibUltralight = "Ultralight";

	static Methods()
	{
		Preload();
	}

	[LibraryImport(LibUltralight)]
	public static partial byte* ulVersionString();

	[LibraryImport(LibUltralight)]
	public static partial uint ulVersionMajor();

	[LibraryImport(LibUltralight)]
	public static partial uint ulVersionMinor();

	[LibraryImport(LibUltralight)]
	public static partial uint ulVersionPatch();

	/// <summary>
	/// Preload Ultralight binaries on OSX/MacOS and Linux
	/// </summary>
	/// <remarks>UltralightCore, WebCore, Ultralight, AppCore</remarks>
	public static void Preload()
	{
		#if NET5_0_OR_GREATER
		if (_resolverRegistered) return;
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) return;
		_resolverRegistered = true;

		// Per-assembly resolver for this assembly
		NativeLibrary.SetDllImportResolver(typeof(Methods).Assembly, ResolveUltralightLibrary);

		// Global last-resort resolver - catches P/Invoke failures from ANY assembly
		// (e.g. old-style [DllImport] in UltralightNet.AppCore)
		System.Runtime.Loader.AssemblyLoadContext.Default.ResolvingUnmanagedDll +=
			(asm, name) => ResolveUltralightLibrary(name, asm, null);
		#endif
	}

	#if NET5_0_OR_GREATER
	private static bool _resolverRegistered;
	private static readonly System.Collections.Generic.HashSet<System.Reflection.Assembly> _registeredAssemblies = new();

	/// <summary>
	/// Register a DLL import resolver for an external assembly (used by UltralightNet.AppCore).
	/// </summary>
	internal static void RegisterResolver(System.Reflection.Assembly assembly)
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) return;
		if (!_registeredAssemblies.Add(assembly)) return;
		NativeLibrary.SetDllImportResolver(assembly, ResolveUltralightLibrary);
	}

	private static string? _nativeDir;

	private static string GetNativeDir()
	{
		if (_nativeDir != null) return _nativeDir;

		string baseDir = AppContext.BaseDirectory;
		if (string.IsNullOrEmpty(baseDir))
			baseDir = Path.GetDirectoryName(typeof(Methods).Assembly.Location) ?? ".";

		string rid = OperatingSystem.IsMacOS() ? "osx-x64" : "linux-x64";
		_nativeDir = Path.Combine(baseDir, "runtimes", rid, "native");
		return _nativeDir;
	}

	private static nint ResolveUltralightLibrary(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
	{
		string? fileName = null;
		if (OperatingSystem.IsLinux())
		{
			fileName = libraryName switch
			{
				"Ultralight" => "libUltralight.so",
				"UltralightCore" => "libUltralightCore.so",
				"WebCore" => "libWebCore.so",
				"AppCore" => "libAppCore.so",
				_ => null
			};
		}
		else if (OperatingSystem.IsMacOS())
		{
			fileName = libraryName switch
			{
				"Ultralight" => "libUltralight.dylib",
				"UltralightCore" => "libUltralightCore.dylib",
				"WebCore" => "libWebCore.dylib",
				"AppCore" => "libAppCore.dylib",
				_ => null
			};
		}

		if (fileName != null)
		{
			string fullPath = Path.Combine(GetNativeDir(), fileName);
			if (NativeLibrary.TryLoad(fullPath, out nint handle))
				return handle;

			// Also try base directory directly (flat layout)
			string baseDir = AppContext.BaseDirectory;
			if (!string.IsNullOrEmpty(baseDir))
			{
				string flatPath = Path.Combine(baseDir, fileName);
				if (NativeLibrary.TryLoad(flatPath, out handle))
					return handle;
			}
		}

		return nint.Zero;
	}
	#endif

	// backported from net8.0 for compatibility
	internal static TTo BitCast<TFrom, TTo>(TFrom from) where TFrom : unmanaged where TTo : unmanaged
	#if !NET8_0_OR_GREATER
	{
		Debug.Assert(sizeof(TFrom) == sizeof(TTo));
		return Unsafe.As<TFrom, TTo>(ref from);
	}
	#else
	=> Unsafe.BitCast<TFrom, TTo>(from);
	#endif
}
