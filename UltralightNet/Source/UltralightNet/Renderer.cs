using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using UltralightNet.Platform;
using UltralightNet.Structs;

namespace UltralightNet;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class Methods
{
	[LibraryImport(LibUltralight)]
	internal static partial void* ulCreateRenderer(in UlConfig config);

	/// <summary>Destroy the renderer.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial void ulDestroyRenderer(Renderer renderer);

	/// <summary>Update timers and dispatch internal callbacks (JavaScript and network).</summary>
	[LibraryImport(LibUltralight)]
	internal static partial void ulUpdate(Renderer renderer);

	/// <summary>Render all active Views.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial void ulRender(Renderer renderer);

	[LibraryImport(LibUltralight)]
	internal static partial void ulRefreshDisplay(Renderer renderer, uint displayId);

	/// <summary>Attempt to release as much memory as possible. Don't call this from any callbacks or driver code.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial void ulPurgeMemory(Renderer renderer);

	[LibraryImport(LibUltralight)]
	internal static partial void ulLogMemoryUsage(Renderer renderer);

	[LibraryImport(LibUltralight, StringMarshalling = StringMarshalling.Utf8)]
	[return: MarshalAs(UnmanagedType.U1)]
	internal static partial bool ulStartRemoteInspectorServer(Renderer renderer, string address, ushort port);

	[LibraryImport(LibUltralight)]
	internal static partial void ulSetGamepadDetails(Renderer renderer, uint index,
		[MarshalUsing(typeof(UlString))] string id, uint axisCount, uint buttonCount);

	[LibraryImport(LibUltralight)]
	internal static partial void ulFireGamepadEvent(Renderer renderer, GamepadEvent* gamepadEvent);

	[LibraryImport(LibUltralight)]
	internal static partial void ulFireGamepadAxisEvent(Renderer renderer, GamepadAxisEvent* gamepadAxisEvent);

	[LibraryImport(LibUltralight)]
	internal static partial void ulFireGamepadButtonEvent(Renderer renderer, GamepadButtonEvent* gamepadButtonEvent);
}

[NativeMarshalling(typeof(Marshaller))]
public sealed unsafe class Renderer : NativeContainer
{
	internal static readonly Dictionary<nuint, WeakReference<Renderer>> Renderers = new(1);
	internal IClipboard.Wrapper? ClipboardWrapper;
	internal IFileSystem.Wrapper? FilesystemWrapper;
	internal IFontLoader.Wrapper? FontLoaderWrapper;
	internal IGpuDriver.Wrapper? GpuDriverWrapper;

	// Soul keepers
	internal ILogger.Wrapper? LoggerWrapper;
	internal ISurfaceDefinition.Wrapper? SurfaceDefinitionWrapper;
	internal readonly Dictionary<nuint, WeakReference<View>> Views = new(1);

	protected override void* Handle
	{
		get
		{
			AssertNotWrongThread();
			return base.Handle;
		}
		init
		{
			Renderers[(nuint)value] = new WeakReference<Renderer>(this);
			base.Handle = value;
		}
	}

	internal int ThreadId { get; set; } = -1;

	public Session DefaultSession => Session.FromHandle(Methods.ulDefaultSession(this), false);

	internal void AssertNotWrongThread() // hungry
	{
		if (ThreadId is not -1 && Platform.Platform.ErrorWrongThread &&
		    ThreadId != Environment.CurrentManagedThreadId)
			throw new AggregateException("Wrong thread. (Platform.ErrorWrongThread)");
	}

	public View CreateView(uint width, uint height, ViewConfig? viewConfig = null, Session? session = null,
		bool dispose = true)
	{
		viewConfig ??= new ViewConfig();
		if (Owns && Platform.Platform.ErrorGpuDriverNotSet && viewConfig.Value.IsAccelerated &&
		    (GpuDriverWrapper?.IsDisposed).GetValueOrDefault(true))
			throw new Exception(
				"No Platform.GPUDriver set, but ViewConfig.IsAccelerated was set to true. (Disable check by setting Platform.ErrorGPUDriverNotSet to false.)");
		var view = View.FromHandle(
			Methods.ulCreateView(this, width, height, viewConfig.Value, session ?? DefaultSession), dispose);
		view.Renderer = this;
		Views[view.GetUserData()] = new WeakReference<View>(view);
		view.SetUpCallbacks();
		return view;
	}

	/// <summary>Create a Session to store local data in (such as cookies, local storage, application cache, indexed db, etc).</summary>
	/// <remarks>
	///     A default, persistent Session is already created for you. You only need to call this if you want to create
	///     private, in-memory session or use a separate session for each View.
	/// </remarks>
	/// <param name="isPersistent">
	///     Whether to store the session on disk.<br />Persistent sessions will be written to
	///     the path set in <see cref="UlConfig.CachePath" />
	/// </param>
	/// <param name="name">
	///     A unique name for this session, this will be used to generate a unique disk path for persistent
	///     sessions.
	/// </param>
	public Session CreateSession(bool isPersistent, string name)
	{
		return Session.FromHandle(Methods.ulCreateSession(this, isPersistent, name), true);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Update()
	{
		Methods.ulUpdate(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Render()
	{
		Methods.ulRender(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RefreshDisplay(uint displayId)
	{
		Methods.ulRefreshDisplay(this, displayId);
	}

	public void PurgeMemory()
	{
		Methods.ulPurgeMemory(this);
	}

	public void LogMemoryUsage()
	{
		Methods.ulLogMemoryUsage(this);
	}

	public bool TryStartRemoteInspectorServer(string address, ushort port)
	{
		return Methods.ulStartRemoteInspectorServer(this, address, port);
	}

	public void SetGamepadDetails(uint index, string id, uint axisCount, uint buttonCount)
	{
		Methods.ulSetGamepadDetails(this, index, id, axisCount, buttonCount);
	}

	public void FireGamepadEvent(GamepadEvent gamepadEvent)
	{
		Methods.ulFireGamepadEvent(this, &gamepadEvent);
	}

	public void FireGamepadAxisEvent(GamepadAxisEvent gamepadAxisEvent)
	{
		Methods.ulFireGamepadAxisEvent(this, &gamepadAxisEvent);
	}

	public void FireGamepadButtonEvent(GamepadButtonEvent gamepadbuttonEvent)
	{
		Methods.ulFireGamepadButtonEvent(this, &gamepadbuttonEvent);
	}

	public override void Dispose()
	{
		if (!IsDisposed && Owns) Methods.ulDestroyRenderer(this);
		GC.KeepAlive(LoggerWrapper);
		GC.KeepAlive(FilesystemWrapper);
		GC.KeepAlive(FontLoaderWrapper);
		GC.KeepAlive(GpuDriverWrapper);
		GC.KeepAlive(SurfaceDefinitionWrapper);
		GC.KeepAlive(ClipboardWrapper);
		base.Dispose();
	}

	internal static Renderer FromHandle(void* handle, bool dispose)
	{
		return new Renderer { Handle = handle, Owns = dispose };
	}

	internal nuint GetCallbackData()
	{
		return (nuint)Handle;
	}

	[CustomMarshaller(typeof(Renderer), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
	internal ref struct Marshaller
	{
		private Renderer _renderer;

		public void FromManaged(Renderer renderer)
		{
			_renderer = renderer;
		}

		public readonly void* ToUnmanaged()
		{
			return _renderer.Handle;
		}

		public readonly void Free()
		{
			GC.KeepAlive(_renderer);
		}
	}
}
