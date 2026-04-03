using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace UltralightNet.AppCore;

public static unsafe partial class AppCoreMethods
{
	[LibraryImport(LibAppCore)]
	internal static unsafe partial void* ulCreateApp(in ULSettings settings, in UlConfig config);

	[LibraryImport(LibAppCore)]
	internal static partial void ulDestroyApp(ULApp app);

	[LibraryImport(LibAppCore)]
	internal static unsafe partial void ulAppSetUpdateCallback(ULApp app,
		delegate* unmanaged[Cdecl]<nuint, void> callback, nuint id);

	[LibraryImport(LibAppCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	internal static partial bool ulAppIsRunning(ULApp app);

	[LibraryImport(LibAppCore)]
	internal static partial void* ulAppGetMainMonitor(ULApp app);

	[LibraryImport(LibAppCore)]
	internal static unsafe partial void* ulAppGetRenderer(ULApp app);

	[LibraryImport(LibAppCore)]
	internal static partial void ulAppRun(ULApp app);

	[LibraryImport(LibAppCore)]
	internal static partial void ulAppQuit(ULApp app);
}

[NativeMarshalling(typeof(Marshaller))]
public sealed unsafe class ULApp : NativeContainer
{
	internal static readonly Dictionary<nuint, WeakReference<ULApp>> Instances = new(1);
	internal readonly Dictionary<nuint, WeakReference<ULWindow>> WindowInstances = new(1);

	private ULApp(void* ptr)
	{
		Handle = ptr;
		Instances[(nuint)Handle] = new WeakReference<ULApp>(this);
		Renderer = Renderer.FromHandle(AppCoreMethods.ulAppGetRenderer(this), false);
		Renderer.ThreadId = Environment.CurrentManagedThreadId;
		AppCoreMethods.ulAppSetUpdateCallback(this, &NativeOnUpdate, GetUserData());
	}

	public Renderer Renderer { get; }

	public bool IsRunning => AppCoreMethods.ulAppIsRunning(this);
	public ULMonitor MainMonitor => ULMonitor.FromHandle(AppCoreMethods.ulAppGetMainMonitor(this), this);
	public event Action? OnUpdate;

	public static ULApp Create(in ULSettings settings, in UlConfig config)
	{
		return new ULApp(AppCoreMethods.ulCreateApp(settings, config));
	}

	public void Run()
	{
		AppCoreMethods.ulAppRun(this);
	}

	public void Quit()
	{
		AppCoreMethods.ulAppQuit(this);
	}

	public override void Dispose()
	{
		if (!IsDisposed && Owns) AppCoreMethods.ulDestroyApp(this);
		base.Dispose();
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void NativeOnUpdate(nuint userData)
	{
		GetApp(userData).OnUpdate?.Invoke();
	}

	internal nuint GetUserData()
	{
		return (nuint)Handle;
	}

	private static ULApp GetApp(nuint userData)
	{
		if (Instances[userData].TryGetTarget(out var app)) return app;

		throw new ObjectDisposedException(nameof(ULApp));
	}

	[CustomMarshaller(typeof(ULApp), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
	internal ref struct Marshaller
	{
		private ULApp app;

		public void FromManaged(ULApp app)
		{
			this.app = app;
		}

		public readonly void* ToUnmanaged()
		{
			return app.Handle;
		}

		public readonly void Free()
		{
			GC.KeepAlive(app);
		}
	}
}
