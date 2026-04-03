using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
[assembly: DisableRuntimeMarshalling]
#endif

namespace UltralightNet.AppCore;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "<Pending>")]
public static unsafe partial class AppCoreMethods
{
	private const string LibAppCore = "AppCore";

	static AppCoreMethods()
	{
		Methods.Preload();
	}

	[LibraryImport(LibAppCore)]
	private static partial void ulEnablePlatformFontLoader();

	public static void SetPlatformFontLoader()
	{
		ulEnablePlatformFontLoader();
		Platform.Platform.SetDefaultFontLoader = false;
	}

	#region ulEnablePlatformFileSystem

	[DllImport("AppCore", EntryPoint = "ulEnablePlatformFileSystem", ExactSpelling = true,
		CallingConvention = CallingConvention.Cdecl)]
	private static extern void ulEnablePlatformFileSystemActual(UlString* baseDirectory);

	public static void ulEnablePlatformFileSystem(UlString* baseDirectory)
	{
		ulEnablePlatformFileSystemActual(baseDirectory);

		Platform.Platform.SetDefaultFileSystem = false;
		Platform.Platform.ErrorMissingResources = false;
	}

	public static void ulEnablePlatformFileSystem(string baseDirectory)
	{
		ulEnablePlatformFileSystem(baseDirectory.AsSpan());
	}

	public static void ulEnablePlatformFileSystem(ReadOnlySpan<char> baseDirectory)
	{
		using UlString baseDirectoryUL = new(baseDirectory);
		ulEnablePlatformFileSystem(&baseDirectoryUL);
	}

	public static void ulEnablePlatformFileSystem(ReadOnlySpan<byte> baseDirectory)
	{
		using UlString baseDirectoryUL = new(baseDirectory);
		ulEnablePlatformFileSystem(&baseDirectoryUL);
	}

	#endregion ulEnablePlatformFileSystem

	#region ulEnableDefaultLogger

	[DllImport("AppCore", EntryPoint = "ulEnableDefaultLogger", ExactSpelling = true,
		CallingConvention = CallingConvention.Cdecl)]
	private static extern void ulEnableDefaultLoggerActual(UlString* logPath);

	public static void ulEnableDefaultLogger(UlString* logPath)
	{
		ulEnableDefaultLoggerActual(logPath);

		Platform.Platform.EnableDefaultLogger = false;
	}

	public static void ulEnableDefaultLogger(string logPath)
	{
		ulEnableDefaultLogger(logPath.AsSpan());
	}

	public static void ulEnableDefaultLogger(ReadOnlySpan<char> logPath)
	{
		using UlString logPathUL = new(logPath);
		ulEnableDefaultLogger(&logPathUL);
	}

	public static void ulEnableDefaultLogger(ReadOnlySpan<byte> logPath)
	{
		using UlString logPathUL = new(logPath);
		ulEnableDefaultLogger(&logPathUL);
	}

	#endregion ulEnableDefaultLogger
}
