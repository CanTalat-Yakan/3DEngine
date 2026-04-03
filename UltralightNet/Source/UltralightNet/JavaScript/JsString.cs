// JSStringRef.h

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace UltralightNet.JavaScript;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Those are 1:1 definitions")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "Compatibility")]
internal unsafe partial class JavaScriptMethods
{
	[LibraryImport(LibWebCore)]
	public static partial JsStringRef JSStringCreateWithCharacters(char* characters, nuint length);

	[LibraryImport(LibWebCore)]
	public static partial JsStringRef JSStringCreateWithUTF8CString(byte* characters);

	/// <summary>Increases ref count</summary>
	[LibraryImport(LibWebCore)]
	public static partial JsStringRef JSStringRetain(JsStringRef @string);

	/// <summary>Decreases ref count</summary>
	[LibraryImport(LibWebCore)]
	public static partial void JSStringRelease(JsStringRef @string);

	[LibraryImport(LibWebCore)]
	public static partial nuint JSStringGetLength(JsStringRef @string);

	[LibraryImport(LibWebCore)]
	public static partial ushort* JSStringGetCharactersPtr(JsStringRef @string);

	[LibraryImport(LibWebCore)]
	public static partial nuint JSStringGetMaximumUTF8CStringSize(JsStringRef @string);

	[LibraryImport(LibWebCore)]
	public static partial nuint JSStringGetUTF8CString(JsStringRef @string, byte* buffer, nuint bufferSize);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSStringIsEqual(JsStringRef a, JsStringRef b);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSStringIsEqualToUTF8CString(JsStringRef str, byte* characters);
}

public readonly struct JsStringRef
{
	#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
	private readonly nuint _handle;
	#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

	public JsStringRef()
	{
		JavaScriptMethods.ThrowUnsupportedConstructor();
	}

	public override int GetHashCode()
	{
		throw JavaScriptMethods.UnsupportedMethodException;
	}

	public override bool Equals(object? o)
	{
		throw JavaScriptMethods.UnsupportedMethodException;
	}

	public static bool operator ==(JsStringRef left, JsStringRef right)
	{
		return left._handle == right._handle;
	}

	public static bool operator !=(JsStringRef left, JsStringRef right)
	{
		return left._handle != right._handle;
	}
}

[DebuggerDisplay("{ToString(),raw}")]
public sealed unsafe class JsString : JsNativeContainer<JsStringRef>, IEquatable<JsString>, IEquatable<string>,
	ICloneable
{
	private JsString()
	{
	}

	public static implicit operator JsStringRef(JsString @string) => @string.JsHandle;

	public nuint Length
	{
		get
		{
			nuint returnValue = JavaScriptMethods.JSStringGetLength(JsHandle);
			GC.KeepAlive(this);
			return returnValue;
		}
	}

	/// <remarks>Use <see cref="GC.KeepAlive(object?)" /> to keep <see cref="JsString" /> instance alive.</remarks>
	public ReadOnlySpan<char> Utf16Data => new(Utf16DataRaw, checked((int)Length));

	/// <remarks>Use <see cref="GC.KeepAlive(object?)" /> to keep <see cref="JsString" /> instance alive.</remarks>
	public char* Utf16DataRaw
	{
		get
		{
			var returnValue = (char*)JavaScriptMethods.JSStringGetCharactersPtr(JsHandle);
			GC.KeepAlive(this);
			return returnValue;
		}
	}
	// do not implement GetPinnableReference because there is UTF16DataRaw

	public nuint MaximumUtf8CStringSize
	{
		get
		{
			nuint returnValue = JavaScriptMethods.JSStringGetMaximumUTF8CStringSize(JsHandle);
			GC.KeepAlive(this);
			return returnValue;
		}
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	public bool Equals(JsString? other)
	{
		if (other is null) return false;
		bool retVal = base.Equals(other) || JavaScriptMethods.JSStringIsEqual(JsHandle, other.JsHandle);
		GC.KeepAlive(this);
		return retVal;
	}

	public bool Equals(string? other)
	{
		if (other is null) return false;
		bool retVal = Utf16Data.SequenceEqual(other.AsSpan());
		GC.KeepAlive(this);
		return retVal;
	}

	public JsString Clone()
	{
		var returnValue = FromHandle(JavaScriptMethods.JSStringRetain(JsHandle), true);
		GC.KeepAlive(this);
		return returnValue;
	}

	public nuint GetUtf8(byte* buffer, nuint bufferSize)
	{
		nuint returnValue = JavaScriptMethods.JSStringGetUTF8CString(JsHandle, buffer, bufferSize);
		GC.KeepAlive(this);
		return returnValue;
	}

	public nuint GetUtf8(Span<byte> buffer)
	{
		fixed (byte* bufferPtr = buffer)
		{
			return GetUtf8(bufferPtr, checked((nuint)buffer.Length));
		}
	}

	public override string ToString()
	{
		string returnValue = new(Utf16DataRaw, 0, checked((int)Length));
		GC.KeepAlive(this);
		return returnValue;
	}

	public static bool operator ==(JsString? left, JsString? right)
	{
		return left is not null ? left.Equals(right) : right is null;
	}

	public static bool operator !=(JsString? left, JsString? right)
	{
		return !(left == right);
	}

	public bool EqualsNullTerminatedUtf8(byte* utf8)
	{
		bool returnValue = JavaScriptMethods.JSStringIsEqualToUTF8CString(JsHandle, utf8);
		GC.KeepAlive(this);
		return returnValue;
	}

	public bool EqualsNullTerminatedUtf8(ReadOnlySpan<byte> utf8)
	{
		if (utf8.Length is 0 || utf8[utf8.Length - 1] is not 0)
			throw new ArgumentException(
				"UTF8 byte span must have null-terminator (\\0) at the end. (If you're sure what you're doing, use byte* overload instead.)",
				nameof(utf8));
		fixed (byte* bytes = utf8)
		{
			return EqualsNullTerminatedUtf8(bytes);
		}
	}

	public static implicit operator JsString(string? str)
	{
		return CreateFromUtf16(str.AsSpan());
	}

	public static explicit operator string(JsString str)
	{
		return str.ToString();
	}

	public static JsString FromHandle(JsStringRef handle, bool dispose)
	{
		return new JsString { JsHandle = handle, Owns = dispose };
	}

	public override void Dispose()
	{
		if (!IsDisposed && Owns) JavaScriptMethods.JSStringRelease(JsHandle);
		base.Dispose();
	}

	public static JsString CreateFromUtf16(char* chars, nuint length)
	{
		return FromHandle(JavaScriptMethods.JSStringCreateWithCharacters(chars, length), true);
	}

	public static JsString CreateFromUtf16(ReadOnlySpan<char> chars)
	{
		fixed (char* characters = chars)
		{
			return FromHandle(JavaScriptMethods.JSStringCreateWithCharacters(characters, (nuint)chars.Length),
				true);
		}
	}
	/*public static JSString CreateFromUTF16Cached(string? @string)
	{
		// on average, 23 times faster than without cache
		@string ??= string.Empty;
		if (Cache.TryGetValue(@string, out var js)) return js.Clone();
		js = CreateFromUtf16(@string.AsSpan());
		Cache.Add(@string, js);
		return js.Clone();
	}*/

	public static JsString CreateFromUtf8NullTerminated(byte* utf8Bytes)
	{
		return FromHandle(JavaScriptMethods.JSStringCreateWithUTF8CString(utf8Bytes), true);
	}

	public static JsString CreateFromUtf8NullTerminated(ReadOnlySpan<byte> utf8)
	{
		if (utf8.Length is 0 || utf8[utf8.Length - 1] is not 0)
			throw new ArgumentException(
				"UTF8 byte span must have null-terminator (\\0) at the end. (If you're sure what you're doing, use byte* overload instead.)",
				nameof(utf8));
		fixed (byte* characters = utf8)
		{
			return CreateFromUtf8NullTerminated(characters);
		}
	}

	//static readonly ConditionalWeakTable<string, JSString> Cache = new();

	public override int GetHashCode()
	{
		return unchecked((int)Length);
	}

	public override bool Equals(object? other)
	{
		return other switch
		{
			JsString js => Equals(js),
			string str => Equals(str),
			_ => false
		};
	}
}
