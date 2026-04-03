using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace UltralightNet;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class Methods
{
	/// <summary>Create string from null-terminated ASCII C-string.</summary>
	[LibraryImport(LibUltralight)]
	public static partial UlString* ulCreateString([MarshalUsing(typeof(Utf8StringMarshaller))] string str);

	/// <summary>Create string from UTF-8 buffer.</summary>
	[LibraryImport(LibUltralight)]
	[Obsolete("Unexpected behaviour")]
	public static partial UlString* ulCreateStringUTF8(
		byte* data,
		nuint len
	);

	/// <summary>Create string from UTF-16 buffer.</summary>
	[LibraryImport(LibUltralight)]
	public static partial UlString* ulCreateStringUTF16([MarshalAs(UnmanagedType.LPWStr)] string str, nuint len);

	/// <summary>Create string from UTF-16 buffer.</summary>
	[LibraryImport(LibUltralight)]
	public static partial UlString* ulCreateStringUTF16(ushort* str, nuint len);

	// <summary>Create string from copy of existing string.</summary>
	public static UlString* ulCreateStringFromCopy(UlString* str)
	{
		// INTEROPTODO: ARM32
		var ulString = (UlString*)NativeMemory.Alloc((nuint)sizeof(UlString));
		*ulString = str->Clone();
		return ulString;
	}

	/// <summary>Destroy string (you should destroy any strings you explicitly Create).</summary>
	[LibraryImport(LibUltralight)]
	public static partial void ulDestroyString(UlString* str);

	/// <summary>Get internal UTF-8 buffer data.</summary>
	[LibraryImport(LibUltralight)]
	public static partial byte* ulStringGetData(UlString* str);

	/// <summary>Get length in UTF-8 characters.</summary>
	[LibraryImport(LibUltralight)]
	public static partial nuint ulStringGetLength(UlString* str);

	/// <summary>Whether this string is empty or not.</summary>
	[LibraryImport(LibUltralight)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool ulStringIsEmpty(UlString* str);

	/// <summary>Replaces the contents of 'str' with the contents of 'new_str'</summary>
	[LibraryImport(LibUltralight)]
	public static partial void ulStringAssignString(UlString* str, UlString* newStr);

	[LibraryImport(LibUltralight)]
	public static partial void ulStringAssignCString(UlString* str, byte* c_str);

	public static void Deallocate(this ref UlString str)
	{
		str.Dispose();
		NativeMemory.Free(Unsafe.AsPointer(ref str));
	}
}

[DebuggerDisplay("{ToString(),raw}")]
[StructLayout(LayoutKind.Sequential)]
[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanaged))]
[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedRef, typeof(ManagedToUnmanaged))]
[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedOut, typeof(UnmanagedToManaged))]
public unsafe struct UlString : IDisposable, ICloneable, IEquatable<UlString>
{
	public byte* data;
	public nuint length;

	public UlString(ReadOnlySpan<char> chars)
	{
		data = (byte*)NativeMemory.Alloc((length = (nuint)Encoding.UTF8.GetByteCount(chars)) + 1);
		var written = (nuint)Encoding.UTF8.GetBytes(chars, new Span<byte>(data, checked((int)length)));
		Debug.Assert(written == length);
		data[length] = 0;
	}

	public UlString(ReadOnlySpan<byte> chars)
	{
		data = (byte*)NativeMemory.Alloc((length = (nuint)chars.Length) + 1);
		chars.CopyTo(new Span<byte>(data, (int)length));
		data[length] = 0;
	}

	public UlString()
	{
		(data = (byte*)NativeMemory.Alloc(1))
			[length = 0] = 0;
	}

	public void Dispose()
	{
		if (data is null) return;
		NativeMemory.Free(data);
		data = null;
		length = 0; // why? string shouldn't even be accessed after disposal! i just do this to notice problems
	}

	public void Assign(ReadOnlySpan<char> newStr)
	{
		data = (byte*)NativeMemory.Alloc((length = (nuint)Encoding.UTF8.GetByteCount(newStr)) + 1);
		var written = (nuint)Encoding.UTF8.GetBytes(newStr, new Span<byte>(data, checked((int)length)));
		Debug.Assert(written == length);
		data[length] = 0;

	}

	public void Assign(ReadOnlySpan<byte> newStr)
	{
		if (data is not null) data = (byte*)NativeMemory.Realloc(data, (length = (nuint)newStr.Length) + 1);
		else data = (byte*)NativeMemory.Alloc((length = (nuint)newStr.Length) + 1);

		newStr.CopyTo(new Span<byte>(data, newStr.Length));

		data[length] = 0;
	}

	public void Assign(UlString newStr)
	{
		if (data is not null) data = (byte*)NativeMemory.Realloc(data, newStr.length + 1);
		else data = (byte*)NativeMemory.Alloc(newStr.length + 1);

		length = newStr.length;

		Buffer.MemoryCopy(data, newStr.data, length, length);

		data[length] = 0;
	}

	public void Assign(UlString* newStr)
	{
		if (data is not null) data = (byte*)NativeMemory.Realloc(data, newStr->length + 1);
		else data = (byte*)NativeMemory.Alloc(newStr->length + 1);

		length = newStr->length;

		Buffer.MemoryCopy(data, newStr->data, length, length);

		data[length] = 0;
	}

	/// <remarks>it doesn't copy</remarks>
	public readonly UlString*
		Allocate() // TODO: implement "UlString* Create(ROS<char/byte>)" that will not require member fields
	{
		var str = (UlString*)NativeMemory.Alloc((nuint)sizeof(UlString));
		str->data = data;
		str->length = length;
		return str;
	}

	public readonly UlString Clone()
	{
		UlString clone = new()
		{
			data = (byte*)NativeMemory.Alloc(length + 1),
			length = length
		};

		Buffer.MemoryCopy(data, clone.data, length, length);

		clone.data[length] = 0;

		return clone;
	}

	readonly object ICloneable.Clone()
	{
		return Clone();
	}

	public readonly bool Equals(UlString str)
	{
		if (length != str.length) return false;
		if (data == str.data) return true;
		if (length < int.MaxValue)
			return new ReadOnlySpan<byte>(data, unchecked((int)length)).SequenceEqual(
				new ReadOnlySpan<byte>(str.data, unchecked((int)length)));
		for (nuint i = 0; i < length; i++)
			if (data[i] != str.data[i])
				return false;
		return true;
	}

	public readonly override bool Equals(object? other)
	{
		return other is UlString str && Equals(str);
	}

	public readonly override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(length);
		hash.AddBytes(new ReadOnlySpan<byte>(data, unchecked((int)Math.Clamp(length, 0, int.MaxValue))));
		return hash.ToHashCode();
	}

	public static explicit operator string(UlString str) => str.data is null || str.length is 0
		? string.Empty
		:
		Marshal.PtrToStringUTF8((IntPtr)str.data, checked((int)str.length));

	public readonly override string ToString()
	{
		return (string)this;
	}

	public readonly ReadOnlySpan<byte> ToSpan()
	{
		return new ReadOnlySpan<byte>(data, checked((int)length));
	}


	internal static class ManagedToUnmanaged
	{
		internal static string ConvertToManaged(UlString* str) => str is null
			? string.Empty
			:
			Marshal.PtrToStringUTF8((IntPtr)str->data, checked((int)str->length));
		internal static UlString* ConvertToUnmanaged(string? managed)
		{
			return new UlString(managed.AsSpan()).Allocate();
		}

		internal static void Free(UlString* unmanaged)
		{
			NativeMemory.Free(unmanaged->data);
			NativeMemory.Free(unmanaged);
		}
	}

	internal static class UnmanagedToManaged
	{
		internal static string ConvertToManaged(UlString* str) => str is null
			? string.Empty
			:
			Marshal.PtrToStringUTF8((IntPtr)str->data, checked((int)str->length));
	}
}
