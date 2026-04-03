using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using UltralightNet.Enums;

namespace UltralightNet;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class Methods
{
	/// <summary>Create empty bitmap.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial UlBitmap ulCreateEmptyBitmap();

	/// <summary>Create bitmap with certain dimensions and pixel format.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial UlBitmap ulCreateBitmap(uint width, uint height, BitmapFormat format);

	/// <summary>Create bitmap from existing pixel buffer. @see Bitmap for help using this function.</summary>
	[LibraryImport(LibUltralight)]
	internal static unsafe partial UlBitmap ulCreateBitmapFromPixels(uint width, uint height, BitmapFormat format,
		uint rowBytes, byte* pixels, nuint size, [MarshalAs(UnmanagedType.U1)] bool shouldCopy);

	/// <summary>Create bitmap from copy.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial UlBitmap ulCreateBitmapFromCopy(UlBitmap existingBitmap);

	/// <summary>
	///     Destroy a bitmap (you should only destroy Bitmaps you have explicitly created via one of the creation
	///     functions above.
	/// </summary>
	[LibraryImport(LibUltralight)]
	internal static partial void ulDestroyBitmap(UlBitmap bitmap);

	/// <summary>Get the width in pixels.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial uint ulBitmapGetWidth(UlBitmap bitmap);

	/// <summary>Get the height in pixels.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial uint ulBitmapGetHeight(UlBitmap bitmap);

	/// <summary>Get the pixel format.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial BitmapFormat ulBitmapGetFormat(UlBitmap bitmap);

	/// <summary>Get the bytes per pixel.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial uint ulBitmapGetBpp(UlBitmap bitmap);

	/// <summary>Get the number of bytes per row.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial uint ulBitmapGetRowBytes(UlBitmap bitmap);

	/// <summary>
	///     Get the size in bytes of the underlying pixel buffer.
	/// </summary>
	[LibraryImport(LibUltralight)]
	internal static partial nuint ulBitmapGetSize(UlBitmap bitmap);

	/// <summary>Whether or not this bitmap owns its own pixel buffer.</summary>
	[LibraryImport(LibUltralight)]
	[return: MarshalAs(UnmanagedType.U1)]
	internal static partial bool ulBitmapOwnsPixels(UlBitmap bitmap);

	/// <summary>Lock pixels for reading/writing, returns pointer to pixel buffer.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial byte* ulBitmapLockPixels(UlBitmap bitmap);

	/// <summary>Unlock pixels after locking.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial void ulBitmapUnlockPixels(UlBitmap bitmap);

	/// <summary>Get raw pixel buffer</summary>
	/// <remarks>you should only call this if Bitmap is already locked.</remarks>
	[LibraryImport(LibUltralight)]
	internal static partial byte* ulBitmapRawPixels(UlBitmap bitmap);

	/// <summary>Whether or not this bitmap is empty.</summary>
	[LibraryImport(LibUltralight)]
	[return: MarshalAs(UnmanagedType.U1)]
	internal static partial bool ulBitmapIsEmpty(UlBitmap bitmap);

	/// <summary>Reset bitmap pixels to 0.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial void ulBitmapErase(UlBitmap bitmap);

	/// <summary>Write bitmap to a PNG on disk.</summary>
	[LibraryImport(LibUltralight)]
	[return: MarshalAs(UnmanagedType.U1)]
	internal static partial bool ulBitmapWritePNG(UlBitmap bitmap,
		[MarshalUsing(typeof(Utf8StringMarshaller))] string path);

	/// <summary>This converts a BGRA bitmap to RGBA bitmap and vice-versa by swapping the red and blue channels.</summary>
	[LibraryImport(LibUltralight)]
	internal static partial void ulBitmapSwapRedBlueChannels(UlBitmap bitmap);
}

#pragma warning disable CS0659
[NativeMarshalling(typeof(Marshaller))]
public sealed unsafe class UlBitmap : NativeContainer, ICloneable, IEquatable<UlBitmap>
#pragma warning restore CS0659
{
	public uint Width => Methods.ulBitmapGetWidth(this);
	public uint Height => Methods.ulBitmapGetHeight(this);

	public BitmapFormat Format => Methods.ulBitmapGetFormat(this);
	public uint Bpp => Methods.ulBitmapGetBpp(this);
	public uint RowBytes => Methods.ulBitmapGetRowBytes(this);
	public nuint Size => Methods.ulBitmapGetSize(this);

	public bool OwnsPixels => Methods.ulBitmapOwnsPixels(this);

	public byte* RawPixels => Methods.ulBitmapRawPixels(this);
	public bool IsEmpty => Methods.ulBitmapIsEmpty(this);

	object ICloneable.Clone()
	{
		return Clone();
	}

	public bool Equals(UlBitmap? other)
	{
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;
		uint height = Height;
		uint width = Width;
		uint bpp = Bpp;
		if (width != other.Width || height != other.Height || Format != other.Format || bpp != other.Bpp ||
		    IsEmpty != other.IsEmpty) return false;

		var rowLength = (int)(bpp * width);

		nuint rowBytes = RowBytes;
		nuint rowBytesOther = other.RowBytes;
		byte* pixels = LockPixels();
		byte* pixelsOther = other.LockPixels();

		var seqEq = true;

		for (nuint y = 0; y < height; y++)
			if (!new ReadOnlySpan<byte>(pixels + rowBytes * y, rowLength).SequenceEqual(
				    new ReadOnlySpan<byte>(pixelsOther + rowBytesOther * y, rowLength)))
			{
				seqEq = false;
				break;
			}

		UnlockPixels();
		other.UnlockPixels();

		return seqEq;
	}

	public byte* LockPixels()
	{
		return Methods.ulBitmapLockPixels(this);
	}

	public void UnlockPixels()
	{
		Methods.ulBitmapUnlockPixels(this);
	}

	public void Erase()
	{
		Methods.ulBitmapErase(this);
	}

	public bool WritePng(string path)
	{
		return Methods.ulBitmapWritePNG(this, path);
	}

	public void SwapRedBlueChannels()
	{
		Methods.ulBitmapSwapRedBlueChannels(this);
	}

	public override void Dispose()
	{
		if (!IsDisposed && Owns) Methods.ulDestroyBitmap(this);
		base.Dispose();
	}

	public UlBitmap Clone()
	{
		return Methods.ulCreateBitmapFromCopy(this);
	}

	public static bool ReferenceEquals(UlBitmap? objA, UlBitmap? objB)
	{
		if (objA is null || objB is null) return objA is null && objB is null;
		if (objA.IsDisposed || objB.IsDisposed) return objA.IsDisposed == objB.IsDisposed;
		return objA.Handle == objB.Handle;
	}

	public override bool Equals(object? other)
	{
		return other is UlBitmap bitmap && Equals(bitmap);
	}

	public override int GetHashCode() => base.GetHashCode();

	public static UlBitmap CreateEmpty()
	{
		return Methods.ulCreateEmptyBitmap();
	}

	public static UlBitmap Create(uint width, uint height, BitmapFormat format)
	{
		return Methods.ulCreateBitmap(width, height, format);
	}

	public static UlBitmap CreateFromPixels(uint width, uint height, BitmapFormat format, uint rowBytes, byte* pixels,
		uint size, bool shouldCopy)
	{
		return Methods.ulCreateBitmapFromPixels(width, height, format, rowBytes, pixels, size, shouldCopy);
	}


	public static UlBitmap FromHandle(void* handle, bool dispose)
	{
		return new UlBitmap { Handle = handle, Owns = dispose };
	}

	[CustomMarshaller(typeof(UlBitmap), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
	[CustomMarshaller(typeof(UlBitmap), MarshalMode.ManagedToUnmanagedOut, typeof(ManagedToUnmanagedOut))]
	internal static class Marshaller
	{
		internal ref struct ManagedToUnmanagedIn
		{
			private UlBitmap _bitmap;

			public void FromManaged(UlBitmap bitmap)
			{
				_bitmap = bitmap;
			}

			public readonly void* ToUnmanaged()
			{
				return _bitmap.Handle;
			}

			public readonly void Free()
			{
				GC.KeepAlive(_bitmap);
			}
		}

		internal ref struct ManagedToUnmanagedOut
		{
			private UlBitmap _bitmap;

			public void FromUnmanaged(void* unmanaged)
			{
				_bitmap = FromHandle(unmanaged, true);
			}

			public readonly UlBitmap ToManaged()
			{
				return _bitmap;
			}

			public readonly void Free()
			{
			}
		}
	}
}
