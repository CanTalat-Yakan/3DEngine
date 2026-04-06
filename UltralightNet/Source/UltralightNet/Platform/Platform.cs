using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UltralightNet.Enums;
using UltralightNet.Handles;
using UltralightNet.Platform.HighPerformance;

namespace UltralightNet
{
	public static partial class Methods
	{
		[LibraryImport("Ultralight")]
		internal static partial void ulPlatformSetLogger(UlLogger logger);

		[LibraryImport("Ultralight")]
		internal static partial void ulPlatformSetFileSystem(UlFileSystem filesystem);

		[LibraryImport("Ultralight")]
		internal static partial void ulPlatformSetFontLoader(UlFontLoader fontLoader);

		[LibraryImport("Ultralight")]
		internal static partial void ulPlatformSetGPUDriver(GpuDriver gpuDriver);

		[LibraryImport("Ultralight")]
		internal static partial void ulPlatformSetSurfaceDefinition(UlSurfaceDefinition surfaceDefinition);

		[LibraryImport("Ultralight")]
		internal static partial void ulPlatformSetClipboard(UlClipboard clipboard);
	}
}

namespace UltralightNet.Platform
{
	[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
	public static unsafe class Platform
	{
		private static ILogger.Wrapper? loggerWrapper;
		private static IFileSystem.Wrapper? filesystemWrapper;
		private static IFontLoader.Wrapper? fontloaderWrapper;
		private static IGpuDriver.Wrapper? gpuDriverWrapper;
		private static ISurfaceDefinition.Wrapper? surfaceDefinitionWrapper;
		private static IClipboard.Wrapper? clipboardWrapper;
		public static bool EnableDefaultLogger { get; set; } = true;

		/// <summary>
		///     Default filesystrem with access to bundled in assembly files (cacert.pem, icudt67l.dat, mediaControls*).
		///     By disabling that (Platform.SetDefaultFileSystem = false), you will face crash, if no filesystem was set.
		/// </summary>
		public static bool SetDefaultFileSystem { get; set; } = true;

		public static bool SetDefaultFontLoader { get; set; } = true;

		public static bool ErrorMissingResources { get; set; } = true;
		public static bool ErrorGpuDriverNotSet { get; set; } = true;

		public static bool ErrorWrongThread { get; set; } = true;

		public static ILogger Logger
		{
			set => Methods.ulPlatformSetLogger((loggerWrapper = new ILogger.Wrapper(value)).NativeStruct);
		}

		public static IFileSystem FileSystem
		{
			set => Methods.ulPlatformSetFileSystem((filesystemWrapper = new IFileSystem.Wrapper(value)).NativeStruct);
		}

		public static IFontLoader FontLoader
		{
			set => Methods.ulPlatformSetFontLoader((fontloaderWrapper = new IFontLoader.Wrapper(value)).NativeStruct);
		}

		public static IGpuDriver GpuDriver
		{
			set => Methods.ulPlatformSetGPUDriver((gpuDriverWrapper = new IGpuDriver.Wrapper(value)).NativeStruct);
		}

		public static ISurfaceDefinition SurfaceDefinition
		{
			set => Methods.ulPlatformSetSurfaceDefinition(
				(surfaceDefinitionWrapper = new ISurfaceDefinition.Wrapper(value))
				.NativeStruct);
		}

		public static IClipboard Clipboard
		{
			set => Methods.ulPlatformSetClipboard((clipboardWrapper = new IClipboard.Wrapper(value)).NativeStruct);
		}

		public static ILogger DefaultLogger => new DefaultConsoleLogger();
		public static IFileSystem DefaultFileSystem => new DefaultResourceOnlyFileSystem();

		public static Renderer CreateRenderer()
		{
			return CreateRenderer(new UlConfig());
		}

		public static Renderer CreateRenderer(UlConfig config, bool dispose = true)
		{
			if (config == default)
				throw new ArgumentException(
					$"You passed default({nameof(UlConfig)}). It's invalid. Use at least \"new {nameof(UlConfig)}()\" instead.",
					nameof(config));

			if (EnableDefaultLogger && loggerWrapper is null) Logger = DefaultLogger;
			if (SetDefaultFileSystem && filesystemWrapper is null)
			{
				FileSystem = config.ResourcePathPrefix is "runtimes/"
					? DefaultFileSystem
					: throw new ArgumentException("Default file system supports only \"runtimes\" ResourcePathPrefix",
						nameof(config));
			}
			else if (ErrorMissingResources && filesystemWrapper is not null)
			{
				#if !NETSTANDARD
				var path = config.ResourcePathPrefix + "icudt67l.dat";
				using UlString str = new(path.AsSpan());
				if (filesystemWrapper.NativeStruct.FileExists is null ||
				    !filesystemWrapper.NativeStruct.FileExists(&str))
					throw new Exception(
						$"{nameof(FileSystem)}.{nameof(IFileSystem.FileExists)}(\"{path}\") returned 'false'. {nameof(UlConfig)}.{nameof(UlConfig.ResourcePathPrefix)} + \"icudt67l.dat\" is required for Renderer creation. (Set {nameof(Platform)}.{nameof(ErrorMissingResources)} to \'false\' to ignore this exception, however, be ready for unhandled crash.)");
				#else
			// throw new PlatformNotSupportedException("We're unable to check presence of required files on netstandard");
				#endif
			}

			if (SetDefaultFontLoader && fontloaderWrapper is null)
				throw new Exception($"{nameof(FontLoader)} not set.");

			var returnValue = Renderer.FromHandle(UltralightNet.Methods.ulCreateRenderer(in config), true);
			returnValue.ThreadId = Environment.CurrentManagedThreadId;

			returnValue.LoggerWrapper = loggerWrapper;
			returnValue.FilesystemWrapper = filesystemWrapper;
			returnValue.FontLoaderWrapper = fontloaderWrapper;
			returnValue.GpuDriverWrapper = gpuDriverWrapper;
			returnValue.SurfaceDefinitionWrapper = surfaceDefinitionWrapper;
			returnValue.ClipboardWrapper = clipboardWrapper;

			return returnValue;
		}

		private sealed class DefaultConsoleLogger : ILogger
		{
			public void LogMessage(LogLevel logLevel, string message)
			{
				foreach (var line in new SpanEnumerator<char>(message.AsSpan(), '\n'))
					Console.WriteLine($"(UL) {logLevel}: {line.ToString()}");
			}
			#if NET5_0_OR_GREATER
			[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
			private static void LogMessage(LogLevel logLevel, UlString* message)
			{
				foreach (ReadOnlySpan<byte> line in new SpanEnumerator<byte>(message->ToSpan(), "\n"u8[0]))
				{
					Console.WriteLine($"(UL) {logLevel}: {Encoding.UTF8.GetString(line)}");
				}
			}

			public UlLogger? GetNativeStruct() => new() { LogMessage = &LogMessage };
			#elif NETSTANDARD2_0
		ULLogger? ILogger.GetNativeStruct() => null;
			#endif

			private ref struct SpanEnumerator<T> where T : IEquatable<T>
			{
				private ReadOnlySpan<T> span;
				private readonly T splitter;

				public SpanEnumerator(ReadOnlySpan<T> span, T splitter)
				{
					this.span = span;
					this.splitter = splitter;
					Current = default;
				}

				public readonly SpanEnumerator<T> GetEnumerator()
				{
					return this;
				}

				public bool MoveNext()
				{
					if (span.Length is 0) return false;
					int index = span.IndexOf(splitter);
					if (index == -1)
					{
						Current = span;
						span = ReadOnlySpan<T>.Empty;
						return true;
					}

					#pragma warning disable IDE0057
					Current = span.Slice(0, index);
					span = span.Slice(index + 1);
					return true;
				}

				public ReadOnlySpan<T> Current { readonly get; private set; }
			}
		}

		private sealed class DefaultResourceOnlyFileSystem : IFileSystem
		{
			public bool FileExists(string path)
			{
				return path switch
				{
					"runtimes/cacert.pem" => true,
					"runtimes/icudt67l.dat" => true,
					_ => false
				};
			}

			public string GetFileCharset(string path)
			{
				return "utf-8";
			}

			public string GetFileMimeType(string path)
			{
				return "application/octet-stream";
			}

			public UlBuffer OpenFile(string path)
			{
				var s = path switch
				{
					"runtimes/cacert.pem" => Resources.Cacertpem,
					"runtimes/icudt67l.dat" => Resources.Icudt67Ldat,
					_ => null
				};
				if (s is UnmanagedMemoryStream unmanagedMemoryStream)
					return UlBuffer.CreateFromOwnedData(unmanagedMemoryStream.PositionPointer,
						checked((nuint)unmanagedMemoryStream.Length));
				if (s is not null)
				{
					var bytes = new byte[s.Length];
					int totalRead = 0;
					while (totalRead < bytes.Length)
					{
						int read = s.Read(bytes, totalRead, bytes.Length - totalRead);
						if (read == 0)
							throw new System.IO.EndOfStreamException($"Unexpected end of stream while reading '{path}'.");
						totalRead += read;
					}
					return UlBuffer.CreateFromDataCopy<byte>(bytes.AsSpan());
				}

				return default;
			}

			#if NET5_0_OR_GREATER
			// TODO: HighPerformance version with utf8 strings
			#elif NETSTANDARD2_0
		ULFileSystem? IFileSystem.GetNativeStruct() => null;
			#endif
		}
	}
}
