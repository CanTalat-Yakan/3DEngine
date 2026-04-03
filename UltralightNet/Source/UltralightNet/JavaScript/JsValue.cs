// JSValueRef.h

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace UltralightNet.JavaScript;

public static class JsValue
{
	/// <summary>
	/// Get the value's <see cref="JsType">type</see>
	/// </summary>
	/// <param name="jsValue">the value ref to examine</param>
	/// <param name="context">the js context to use</param>
	/// <returns>The provided value's <see cref="JsType">type</see></returns>
	public static JsType GetType(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueGetType(context, jsValue);

	/// <summary>
	/// Check whether the <see cref="JsValueRef">value</see> is undefined
	/// </summary>
	/// <param name="jsValue">the value ref to examine</param>
	/// <param name="context">the js context to use</param>
	/// <returns>A boolean</returns>
	public static bool IsUndefined(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueIsUndefined(context, jsValue);

	/// <summary>
	/// Check whether the <see cref="JsValueRef">value</see> is null
	/// </summary>
	/// <param name="jsValue">the value ref to examine</param>
	/// <param name="context">the js context to use</param>
	/// <returns>A boolean</returns>
	public static bool IsNull(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueIsNull(context, jsValue);

	/// <summary>
	/// Check whether the <see cref="JsValueRef">value</see> is a boolean
	/// </summary>
	/// <param name="jsValue">the value ref to examine</param>
	/// <param name="context">the js context to use</param>
	/// <returns>A boolean</returns>
	public static bool IsBoolean(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueIsBoolean(context, jsValue);

	/// <summary>
	/// Check whether the <see cref="JsValueRef">value</see> is a number
	/// </summary>
	/// <param name="jsValue">the value ref to examine</param>
	/// <param name="context">the js context to use</param>
	/// <returns>A boolean</returns>
	public static bool IsNumber(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueIsNumber(context, jsValue);

	/// <summary>
	/// Check whether the <see cref="JsValueRef">value</see> is string
	/// </summary>
	/// <param name="jsValue">the value ref to examine</param>
	/// <param name="context">the js context to use</param>
	/// <returns>A boolean</returns>
	public static bool IsString(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueIsString(context, jsValue);

	/// <summary>
	/// Check whether the <see cref="JsValueRef">value</see> is a symbol
	/// </summary>
	/// <param name="jsValue">the value ref to examine</param>
	/// <param name="context">the js context to use</param>
	/// <returns>A boolean</returns>
	public static bool IsSymbol(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueIsSymbol(context, jsValue);

	/// <summary>
	/// Check whether the <see cref="JsValueRef">value</see> is an <see cref="JsObjectRef">object</see>
	/// </summary>
	/// <param name="jsValue">the value ref to examine</param>
	/// <param name="context">the js context to use</param>
	/// <returns>A boolean</returns>
	public static bool IsObject(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueIsObject(context, jsValue);

	/// <summary>
	/// Check whether the <see cref="JsValueRef">value</see> is undefined
	/// </summary>
	/// <param name="jsValue">the value ref to examine</param>
	/// <param name="context">the js context to use</param>
	/// <returns>A boolean</returns>
	public static bool IsArray(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueIsArray(context, jsValue);

	/// <summary>
	/// Check whether the <see cref="JsValueRef">value</see> is an object of the specified <see cref="JsClassRef">class</see>
	/// </summary>
	/// <param name="jsValue">the value ref to examine</param>
	/// <param name="context">the js context to use</param>
	/// <param name="jsClass">a js class</param>
	/// <returns>A boolean</returns>
	public static bool IsObjectOfClass(this JsValueRef jsValue, JsContextRef context, JsClassRef jsClass) =>
		JavaScriptMethods.JSValueIsObjectOfClass(context, jsValue, jsClass);

	/// <summary>
	/// Check whether the <see cref="JsValueRef">value</see> is undefined
	/// </summary>
	/// <param name="jsValue">the value ref to examine</param>
	/// <param name="context">the js context to use</param>
	/// <returns>A boolean</returns>
	public static bool IsDate(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueIsDate(context, jsValue);

	/// <summary>
	/// Check the <see cref="JsTypedArrayType">array type</see> of the <see cref="JsValueRef">value</see>
	/// </summary>
	/// <param name="jsValue">the value ref to examine</param>
	/// <param name="context">the js context to use</param>
	/// <returns>An <see cref="JsTypedArrayType">array type</see></returns>
	public static unsafe JsTypedArrayType GetTypedArrayType(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueGetTypedArrayType(context, jsValue);

	/// <summary>
	/// Check whether the two <see cref="JsValueRef">value</see>s are equal.
	/// Equivalent to JavaScript's <c>a == b</c>.
	/// </summary>
	/// <param name="context">the js context to use</param>
	/// <param name="left">the first value ref</param>
	/// <param name="right">the second value ref</param>
	/// <returns>A boolean</returns>
	public static unsafe bool IsEqual(JsContextRef context, JsValueRef left, JsValueRef right) =>
		JavaScriptMethods.JSValueIsEqual(context, left, right);

	/// <summary>
	/// Check whether the two <see cref="JsValueRef">value</see>s are strictly equal.
	/// Equivalent to JavaScript's <c>a === b</c>.
	/// </summary>
	/// <param name="context">the js context to use</param>
	/// <param name="left">the first value ref</param>
	/// <param name="right">the second value ref</param>
	/// <returns>A boolean</returns>
	public static bool IsStrictEqual(JsContextRef context, JsValueRef left, JsValueRef right) =>
		JavaScriptMethods.JSValueIsStrictEqual(context, left, right);

	/// <summary>
	/// Check whether the <see cref="JsValueRef">value</see> is an instance of a constructor
	/// </summary>
	/// <param name="jsValue">the value ref to examine</param>
	/// <param name="context">the js context to use</param>
	/// <param name="constructor">the constructor to check against</param>
	/// <returns>A boolean</returns>
	public static unsafe bool IsInstanceOfConstructor(this JsValueRef jsValue, JsContextRef context, JsObjectRef constructor) =>
		JavaScriptMethods.JSValueIsInstanceOfConstructor(context, jsValue, constructor);

	/// <summary>
	/// Creates an undefined <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="context">the js context to use</param>
	/// <returns>A <see cref="JsValueRef">value reference</see> to <c>undefined</c></returns>
	public static JsValueRef MakeUndefined(JsContextRef context) =>
		JavaScriptMethods.JSValueMakeUndefined(context);

	/// <summary>
	/// Creates an null <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="context">the js context to use</param>
	/// <returns>A <see cref="JsValueRef">value reference</see> to <c>null</c></returns>
	public static JsValueRef MakeNull(JsContextRef context) =>
		JavaScriptMethods.JSValueMakeNull(context);

	/// <summary>
	/// Creates a boolean <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="context">the js context to use</param>
	/// <param name="value">the bool value</param>
	/// <returns>A <see cref="JsValueRef">value reference</see></returns>
	public static JsValueRef MakeBoolean(JsContextRef context, bool value) =>
		JavaScriptMethods.JSValueMakeBoolean(context, value);

	/// <summary>
	/// Creates a number <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="context">the js context to use</param>
	/// <param name="value">the number value</param>
	/// <returns>A <see cref="JsValueRef">value reference</see></returns>
	public static JsValueRef MakeNumber<T>(JsContextRef context, T value) where T : INumber<T> =>
		JavaScriptMethods.JSValueMakeNumber(context, Convert.ToDouble(value));

	/// <summary>
	/// Creates a number <see cref="JsValueRef">value reference</see>, specifically a <see cref="double"/>
	/// </summary>
	/// <param name="context">the js context to use</param>
	/// <param name="value">the <see cref="double"/> value</param>
	/// <returns>A <see cref="JsValueRef">value reference</see></returns>
	public static JsValueRef MakeNumber(JsContextRef context, double value) =>
		JavaScriptMethods.JSValueMakeNumber(context, value);

	/// <summary>
	/// Creates a string <see cref="JsValueRef">value reference</see> from a JavaScript string reference
	/// </summary>
	/// <param name="context">the js context to use</param>
	/// <param name="jsString">the string reference</param>
	/// <returns>A <see cref="JsValueRef">value reference</see></returns>
	public static JsValueRef MakeString(JsContextRef context, JsStringRef jsString) =>
		JavaScriptMethods.JSValueMakeString(context, jsString);

	/// <summary>
	/// Creates a string <see cref="JsValueRef">value reference</see> from a UTF-16 string
	/// </summary>
	/// <param name="context">the js context to use</param>
	/// <param name="value">the string to use</param>
	/// <returns>A <see cref="JsString"/> for disposal along with a <see cref="JsValueRef">value reference</see></returns>
	public static (JsString, JsValueRef) MakeString(JsContextRef context, string value)
	{
		var jsString = JsString.CreateFromUtf16(value);
		var stringRef = JavaScriptMethods.JSValueMakeString(context, jsString.JsHandle);
		return (jsString, stringRef);
	}

	/// <summary>
	/// Creates a JavaScript symbol <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="context">the js context to use</param>
	/// <param name="description">a description of the symbol</param>
	/// <returns>A <see cref="JsValueRef">value reference</see></returns>
	public static JsValueRef MakeSymbol(JsContextRef context, JsStringRef description) =>
		JavaScriptMethods.JSValueMakeSymbol(context, description);

	/// <summary>
	/// Creates a JavaScript <see cref="JsValueRef">value reference</see> by parsing a JavaScript object
	/// </summary>
	/// <param name="context">the js context to use</param>
	/// <param name="json">the json contents</param>
	/// <returns>A <see cref="JsValueRef">value reference</see></returns>
	public static JsValueRef MakeFromJsonString(JsContextRef context, JsStringRef json) =>
		JavaScriptMethods.JSValueMakeFromJSONString(context, json);

	/// <summary>
	/// Creates a JSON representation of the <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="context">the js context to use</param>
	/// <param name="jsValue">the value ref to use</param>
	/// <param name="indent">optional indentation</param>
	/// <returns>A <see cref="JsStringRef">string reference</see></returns>
	public static unsafe JsStringRef CreateJsonString(JsContextRef context, JsValueRef jsValue,
		uint indent = 0) =>
		JavaScriptMethods.JSValueCreateJSONString(context, jsValue, indent);

	/// <summary>
	/// Creates a boolean representation of the <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="jsValue">the value ref to use</param>
	/// <param name="context">the js context to use</param>
	/// <returns>The <see cref="JsValueRef">value reference</see>'s boolean value</returns>
	public static bool ToBoolean(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueToBoolean(context, jsValue);

	/// <summary>
	/// Creates a double representation of the <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="jsValue">the value ref to use</param>
	/// <param name="context">the js context to use</param>
	/// <returns>The <see cref="JsValueRef">value reference</see>'s double value</returns>
	public static unsafe double ToDouble(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueToNumber(context, jsValue);

	/// <summary>
	/// Creates a float representation of the <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="jsValue">the value ref to use</param>
	/// <param name="context">the js context to use</param>
	/// <returns>The <see cref="JsValueRef">value reference</see>'s float value</returns>
	public static unsafe float ToFloat(this JsValueRef jsValue, JsContextRef context) =>
		(float)JavaScriptMethods.JSValueToNumber(context, jsValue);

	/// <summary>
	/// Creates a short representation of the <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="jsValue">the value ref to use</param>
	/// <param name="context">the js context to use</param>
	/// <returns>The <see cref="JsValueRef">value reference</see>'s short value</returns>
	public static unsafe short ToInt16(this JsValueRef jsValue, JsContextRef context) =>
		(short)JavaScriptMethods.JSValueToNumber(context, jsValue);

	/// <summary>
	/// Creates an ushort representation of the <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="jsValue">the value ref to use</param>
	/// <param name="context">the js context to use</param>
	/// <returns>The <see cref="JsValueRef">value reference</see>'s ushort value</returns>
	public static unsafe ushort ToUInt16(this JsValueRef jsValue, JsContextRef context) =>
		(ushort)JavaScriptMethods.JSValueToNumber(context, jsValue);

	/// <summary>
	/// Creates an int representation of the <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="jsValue">the value ref to use</param>
	/// <param name="context">the js context to use</param>
	/// <returns>The <see cref="JsValueRef">value reference</see>'s int value</returns>
	public static unsafe int ToInt32(this JsValueRef jsValue, JsContextRef context) =>
		(int)JavaScriptMethods.JSValueToNumber(context, jsValue);

	/// <summary>
	/// Creates an uint representation of the <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="jsValue">the value ref to use</param>
	/// <param name="context">the js context to use</param>
	/// <returns>The <see cref="JsValueRef">value reference</see>'s uint value</returns>
	public static unsafe uint ToUInt32(this JsValueRef jsValue, JsContextRef context) =>
		(uint)JavaScriptMethods.JSValueToNumber(context, jsValue);

	/// <summary>
	/// Creates a long representation of the <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="jsValue">the value ref to use</param>
	/// <param name="context">the js context to use</param>
	/// <returns>The <see cref="JsValueRef">value reference</see>'s long value</returns>
	public static unsafe long ToInt64(this JsValueRef jsValue, JsContextRef context) =>
		(long)JavaScriptMethods.JSValueToNumber(context, jsValue);

	/// <summary>
	/// Creates an ulong representation of the <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="jsValue">the value ref to use</param>
	/// <param name="context">the js context to use</param>
	/// <returns>The <see cref="JsValueRef">value reference</see>'s ulong value</returns>
	public static unsafe ulong ToUInt64(this JsValueRef jsValue, JsContextRef context) =>
		(ulong)JavaScriptMethods.JSValueToNumber(context, jsValue);

	/// <summary>
	/// Creates a copy of the <see cref="JsValueRef">value reference</see>'s string value
	/// </summary>
	/// <param name="jsValue">the value ref to use</param>
	/// <param name="context">the js context to use</param>
	/// <returns>The <see cref="JsValueRef">value reference</see>'s string value</returns>
	public static unsafe JsStringRef ToStringCopy(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueToStringCopy(context, jsValue);

	/// <summary>
	/// Creates a copy of the <see cref="JsValueRef">value reference</see>'s string value
	/// </summary>
	/// <param name="jsValue">the value ref to use</param>
	/// <param name="context">the js context to use</param>
	/// <returns>The <see cref="JsValueRef">value reference</see>'s string value</returns>
	public static unsafe string ToUtf16StringCopy(this JsValueRef jsValue, JsContextRef context)
	{
		var jsString = JsString.FromHandle(JavaScriptMethods.JSValueToStringCopy(context, jsValue), true);
		var str = jsString.ToString();
		jsString.Dispose();
		return str;
	}

	/// <summary>
	/// Creates a JavaScript object representation of the <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="jsValue">the value ref to use</param>
	/// <param name="context">the js context to use</param>
	/// <returns>The <see cref="JsValueRef">value reference</see>'s ulong value</returns>
	public static unsafe JsObjectRef ToObject(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueToObject(context, jsValue);

	/// <summary>
	/// Protects a <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="jsValue">the value ref to protect</param>
	/// <param name="context">the js context to use</param>
	public static void Protect(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueProtect(context, jsValue);

	/// <summary>
	/// Unprotects a <see cref="JsValueRef">value reference</see>
	/// </summary>
	/// <param name="jsValue">the value ref to unprotect</param>
	/// <param name="context">the js context to use</param>
	public static void Unprotect(this JsValueRef jsValue, JsContextRef context) =>
		JavaScriptMethods.JSValueUnprotect(context, jsValue);
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Those are 1:1 definitions")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "Compatibility")]
internal unsafe partial class JavaScriptMethods
{
	[LibraryImport(LibWebCore)]
	public static partial JsType JSValueGetType(JsContextRef context, JsValueRef jsValue);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueIsUndefined(JsContextRef context, JsValueRef jsValue);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueIsNull(JsContextRef context, JsValueRef jsValue);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueIsBoolean(JsContextRef context, JsValueRef jsValue);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueIsNumber(JsContextRef context, JsValueRef jsValue);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueIsString(JsContextRef context, JsValueRef jsValue);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueIsSymbol(JsContextRef context, JsValueRef jsValue);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueIsObject(JsContextRef context, JsValueRef jsValue);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueIsObjectOfClass(JsContextRef context, JsValueRef jsValue,
		JsClassRef jsClass);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueIsArray(JsContextRef context, JsValueRef jsValue);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueIsDate(JsContextRef context, JsValueRef jsValue);

	[LibraryImport(LibWebCore)]
	public static partial JsTypedArrayType JSValueGetTypedArrayType(JsContextRef context, JsValueRef jsValue,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueIsEqual(JsContextRef context, JsValueRef a, JsValueRef b,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueIsStrictEqual(JsContextRef context, JsValueRef a, JsValueRef b);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueIsInstanceOfConstructor(JsContextRef context, JsValueRef jsValue,
		JsObjectRef constructor, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSValueMakeUndefined(JsContextRef context);

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSValueMakeNull(JsContextRef context);

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSValueMakeBoolean(JsContextRef context,
		[MarshalAs(UnmanagedType.U1)] bool boolean);

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSValueMakeNumber(JsContextRef context, double number);

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSValueMakeString(JsContextRef context, JsStringRef jsString);

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSValueMakeSymbol(JsContextRef context, JsStringRef description);

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSValueMakeFromJSONString(JsContextRef context, JsStringRef jsString);

	[LibraryImport(LibWebCore)]
	public static partial JsStringRef JSValueCreateJSONString(JsContextRef context, JsValueRef jsValue,
		uint indent, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSValueToBoolean(JsContextRef context, JsValueRef jsValue);

	[LibraryImport(LibWebCore)]
	public static partial double JSValueToNumber(JsContextRef context, JsValueRef jsValue,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsStringRef JSValueToStringCopy(JsContextRef context, JsValueRef jsValue,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSValueToObject(JsContextRef context, JsValueRef jsValue,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial void JSValueProtect(JsContextRef context, JsValueRef jsValue);

	[LibraryImport(LibWebCore)]
	public static partial void JSValueUnprotect(JsContextRef context, JsValueRef jsValue);
}

/// <summary>
/// A JavaScript value. The base type for all JavaScript values, and polymorphic functions on them.
/// </summary>
public readonly struct JsValueRef
{
	#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
	private readonly UIntPtr _handle;
	#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

	public JsValueRef()
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

	public static bool operator ==(JsValueRef left, JsValueRef right)
	{
		return left._handle == right._handle;
	}

	public static bool operator !=(JsValueRef left, JsValueRef right)
	{
		return left._handle != right._handle;
	}
}

/// <summary>
/// A constant identifying the type of JsValue.
/// </summary>
public enum JsType
{
	/// <summary>
	/// The unique undefined value.
	/// </summary>
	Undefined,
	/// <summary>
	/// The unique null value.
	/// </summary>
	Null,
	/// <summary>
	/// A primitive boolean value, one of true or false.
	/// </summary>
	Boolean,
	/// <summary>
	/// A primitive number value.
	/// </summary>
	Number,
	/// <summary>
	/// A primitive string value.
	/// </summary>
	String,
	/// <summary>
	/// An object value (meaning that this JsValueRef is a JsObjectRef).
	/// </summary>
	Object,
	/// <summary>
	///  A primitive symbol value.
	/// </summary>
	Symbol
}

/// <summary>
/// A constant identifying the Typed Array type of JsObjectRef.
/// </summary>
public enum JsTypedArrayType
{
	Int8Array,
	Int16Array,
	Int32Array,
	Uint8Array,
	Uint8ClampedArray,
	Uint16Array,
	Uint32Array,
	Float32Array,
	Float64Array,
	ArrayBuffer,
	/// <summary>
	/// Not a Typed Array
	/// </summary>
	None
}
