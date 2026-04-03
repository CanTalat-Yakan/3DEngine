using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace UltralightNet.AppCore;

public static partial class AppCoreMethods
{
	[LibraryImport(LibAppCore)]
	public static partial double ulMonitorGetScale(ULMonitor monitor);

	[LibraryImport(LibAppCore)]
	public static partial uint ulMonitorGetWidth(ULMonitor monitor);

	[LibraryImport(LibAppCore)]
	public static partial uint ulMonitorGetHeight(ULMonitor monitor);
}

[NativeMarshalling(typeof(Marshaller))]
public sealed unsafe class ULMonitor : NativeContainer
{
	internal readonly ULApp app;

	private ULMonitor(void* ptr, ULApp app)
	{
		Handle = ptr;
		this.app = app;
	}

	public double Scale => AppCoreMethods.ulMonitorGetScale(this);
	public uint Width => AppCoreMethods.ulMonitorGetWidth(this);
	public uint Height => AppCoreMethods.ulMonitorGetHeight(this);

	public ULWindow CreateWindow(uint width, uint height, bool fullscreen = false,
		ULWindowFlags flags = ULWindowFlags.Titled | ULWindowFlags.Resizable | ULWindowFlags.Maximizable)
	{
		if (flags.HasFlag(ULWindowFlags.Borderless) &&
		    (flags.HasFlag(ULWindowFlags.Maximizable) || flags.HasFlag(ULWindowFlags.Titled)))
			throw new ArgumentException("Invalid combination of flags.", nameof(flags));
		var window = ULWindow.FromHandle(AppCoreMethods.ulCreateWindow(this, width, height, fullscreen, flags), app);
		return window;
	}

	internal static ULMonitor FromHandle(void* ptr, ULApp app)
	{
		return new ULMonitor(ptr, app);
	}

	public override void Dispose()
	{
		GC.KeepAlive(app);
		base.Dispose();
	}

	[CustomMarshaller(typeof(ULMonitor), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
	internal ref struct Marshaller
	{
		private ULMonitor monitor;

		public void FromManaged(ULMonitor monitor)
		{
			this.monitor = monitor;
		}

		public readonly void* ToUnmanaged()
		{
			return monitor.Handle;
		}

		public readonly void Free()
		{
			GC.KeepAlive(monitor);
		}
	}
}
