using System.Runtime.InteropServices;

namespace UltralightNet.Platform;

internal static class Helper
{
	public static nint AllocateDelegate<TDelegate>(TDelegate d, out GCHandle handle) where TDelegate : Delegate
	{
		handle = GCHandle.Alloc(d);
		return Marshal.GetFunctionPointerForDelegate(d);
	}
}
