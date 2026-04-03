using System.Runtime.InteropServices;
using UltralightNet.Structs;

namespace UltralightNet;

public static unsafe partial class Methods
{
	/// <summary>Width (in pixels).</summary>
	[LibraryImport("Ultralight")]
	internal static partial uint ulSurfaceGetWidth(nuint surface);

	/// <summary>Height (in pixels).</summary>
	[LibraryImport("Ultralight")]
	internal static partial uint ulSurfaceGetHeight(nuint surface);

	/// <summary>Number of bytes between rows (usually width * 4)</summary>
	[LibraryImport("Ultralight")]
	internal static partial uint ulSurfaceGetRowBytes(nuint surface);

	/// <summary>Size in bytes.</summary>
	[LibraryImport("Ultralight")]
	internal static partial nuint ulSurfaceGetSize(nuint surface);

	/// <summary>
	///     Lock the pixel buffer and get a pointer to the beginning of the data
	///     for reading/writing.
	///     <br />
	///     Native pixel format is premultiplied BGRA 32-bit (8 bits per channel).
	/// </summary>
	[LibraryImport("Ultralight")]
	internal static partial byte* ulSurfaceLockPixels(nuint surface);

	/// <summary>Unlock the pixel buffer.</summary>
	[LibraryImport("Ultralight")]
	internal static partial void ulSurfaceUnlockPixels(nuint surface);

	/// <summary>Resize the pixel buffer to a certain width and height (both in pixels).</summary>
	[LibraryImport("Ultralight")]
	internal static partial void ulSurfaceResize(nuint surface, uint width, uint height);

	/// <summary>Set the dirty bounds to a certain value.</summary>
	[LibraryImport("Ultralight")]
	internal static partial void ulSurfaceSetDirtyBounds(nuint surface, UlIntRect bounds);

	/// <summary>Get the dirty bounds.</summary>
	// todo: example code
	[LibraryImport("Ultralight")]
	internal static partial UlIntRect ulSurfaceGetDirtyBounds(nuint surface);

	/// <summary>Clear the dirty bounds.</summary>
	[LibraryImport("Ultralight")]
	internal static partial void ulSurfaceClearDirtyBounds(nuint surface);

	/// <summary>
	///     Get the underlying user data pointer (this is only valid if you have set a custom surface implementation via
	///     ulPlatformSetSurfaceDefinition).
	/// </summary>
	[LibraryImport("Ultralight")]
	internal static partial nint ulSurfaceGetUserData(nuint surface);

	/// <summary>Get the underlying Bitmap from the default Surface.</summary>
	[LibraryImport("Ultralight")]
	internal static partial void* ulBitmapSurfaceGetBitmap(nuint surface);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct Surface
{
	private readonly nuint Ptr;

	private Surface(nuint handle)
	{
		Ptr = handle;
	}

	public uint Width => Methods.ulSurfaceGetWidth(Ptr);
	public uint Height => Methods.ulSurfaceGetHeight(Ptr);

	public uint RowBytes => Methods.ulSurfaceGetRowBytes(Ptr);
	public nuint Size => Methods.ulSurfaceGetSize(Ptr);

	public unsafe byte* LockPixels()
	{
		return Methods.ulSurfaceLockPixels(Ptr);
	}

	public void UnlockPixels()
	{
		Methods.ulSurfaceUnlockPixels(Ptr);
	}

	public void Resize(uint width, uint height)
	{
		Methods.ulSurfaceResize(Ptr, width, height);
	}

	public UlIntRect DirtyBounds
	{
		get => Methods.ulSurfaceGetDirtyBounds(Ptr);
		set => Methods.ulSurfaceSetDirtyBounds(Ptr, value);
	}

	public void ClearDirtyBounds()
	{
		Methods.ulSurfaceClearDirtyBounds(Ptr);
	}

	public nint Id => Methods.ulSurfaceGetUserData(Ptr);

	public unsafe UlBitmap? Bitmap
	{
		get
		{
			var handle = Methods.ulBitmapSurfaceGetBitmap(Ptr);
			return handle is not null ? UlBitmap.FromHandle(handle, false) : null;
		}
	}

	internal static Surface FromHandle(nuint handle)
	{
		return new Surface(handle);
	}
}
