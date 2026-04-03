using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace UltralightNet.JavaScript;

public static class JsPropertyNameArray
{
	public static JsPropertyNameArrayRef Retain(this JsPropertyNameArrayRef array) =>
		JavaScriptMethods.JSPropertyNameArrayRetain(array);

	public static void Release(this JsPropertyNameArrayRef array) =>
		JavaScriptMethods.JSPropertyNameArrayRelease(array);

	public static UIntPtr GetCount(this JsPropertyNameArrayRef array) =>
		JavaScriptMethods.JSPropertyNameArrayGetCount(array);

	public static JsStringRef GetNameAtIndex(this JsPropertyNameArrayRef array, UIntPtr index) =>
		JavaScriptMethods.JSPropertyNameArrayGetNameAtIndex(array, index);
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Those are 1:1 definitions")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "Compatibility")]
internal partial class JavaScriptMethods
{
	[LibraryImport(LibWebCore)]
	public static partial JsPropertyNameArrayRef JSPropertyNameArrayRetain(JsPropertyNameArrayRef array);

	[LibraryImport(LibWebCore)]
	public static partial void JSPropertyNameArrayRelease(JsPropertyNameArrayRef array);

	[LibraryImport(LibWebCore)]
	public static partial nuint JSPropertyNameArrayGetCount(JsPropertyNameArrayRef array);

	[LibraryImport(LibWebCore)]
	public static partial JsStringRef JSPropertyNameArrayGetNameAtIndex(JsPropertyNameArrayRef array, nuint index);
}

public readonly struct JsPropertyNameArrayRef
{
	#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
	private readonly nuint _handle;
	#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

	public JsPropertyNameArrayRef()
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

	public static bool operator ==(JsPropertyNameArrayRef left, JsPropertyNameArrayRef right)
	{
		return left._handle == right._handle;
	}

	public static bool operator !=(JsPropertyNameArrayRef left, JsPropertyNameArrayRef right)
	{
		return left._handle != right._handle;
	}
}
