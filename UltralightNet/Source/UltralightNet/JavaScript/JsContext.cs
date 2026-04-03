// JSContextRef.h

using System.Runtime.InteropServices;

namespace UltralightNet.JavaScript;

public static class JsContext
{
	public static JsObjectRef GetGlobalObject(this JsContextRef context) =>
		JavaScriptMethods.JSContextGetGlobalObject(context);
	public static JsContextGroupRef GetGroup(this JsContextRef context) =>
		JavaScriptMethods.JSContextGetGroup(context);
	public static JsGlobalContextRef GetGlobalContext(this JsContextRef context) =>
		JavaScriptMethods.JSContextGetGlobalContext(context);
}

internal partial class JavaScriptMethods
{
	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSContextGetGlobalObject(JsContextRef context);

	[LibraryImport(LibWebCore)]
	public static partial JsContextGroupRef JSContextGetGroup(JsContextRef context);

	[LibraryImport(LibWebCore)]
	public static partial JsGlobalContextRef JSContextGetGlobalContext(JsContextRef context);
}

public readonly struct JsContextRef
{
	#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
	private readonly nuint _handle;
	#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

	public JsContextRef()
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

	public static bool operator ==(JsContextRef left, JsContextRef right)
	{
		return left._handle == right._handle;
	}

	public static bool operator !=(JsContextRef left, JsContextRef right)
	{
		return left._handle != right._handle;
	}
}
