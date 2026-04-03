using System.Runtime.InteropServices;

namespace UltralightNet.JavaScript;

public static class JsGlobalContext
{
	public static JsGlobalContextRef Create(JsClassRef globalObjectClass = default) =>
		JavaScriptMethods.JSGlobalContextCreate(globalObjectClass);

	public static JsGlobalContextRef CreateInGroup(JsContextGroupRef contextGroup = default, JsClassRef globalObjectClass = default) =>
		JavaScriptMethods.JSGlobalContextCreateInGroup(contextGroup, globalObjectClass);

	public static JsGlobalContextRef Retain(this JsGlobalContextRef globalContext) =>
		JavaScriptMethods.JSGlobalContextRetain(globalContext);

	public static void Release(this JsGlobalContextRef globalContext) =>
		JavaScriptMethods.JSGlobalContextRelease(globalContext);

	public static JsStringRef CopyName(this JsGlobalContextRef globalContext) =>
		JavaScriptMethods.JSGlobalContextCopyName(globalContext);

	public static void SetName(this JsGlobalContextRef globalContext, JsStringRef name) =>
		JavaScriptMethods.JSGlobalContextSetName(globalContext, name);
}

internal partial class JavaScriptMethods
{
	[LibraryImport(LibWebCore)]
	public static partial JsGlobalContextRef JSGlobalContextCreate(JsClassRef globalObjectClass = default);

	[LibraryImport(LibWebCore)]
	public static partial JsGlobalContextRef JSGlobalContextCreateInGroup(
		JsContextGroupRef contextGroup = default, JsClassRef globalObjectClass = default);

	[LibraryImport(LibWebCore)]
	public static partial JsGlobalContextRef JSGlobalContextRetain(JsGlobalContextRef globalContext);

	[LibraryImport(LibWebCore)]
	public static partial void JSGlobalContextRelease(JsGlobalContextRef globalContext);

	[LibraryImport(LibWebCore)]
	public static partial JsStringRef JSGlobalContextCopyName(JsGlobalContextRef globalContext);

	[LibraryImport(LibWebCore)]
	public static partial void JSGlobalContextSetName(JsGlobalContextRef globalContext, JsStringRef name);
}

public readonly struct JsGlobalContextRef
{
	#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
	private readonly nuint _handle;
	#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

	public JsGlobalContextRef()
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

	public static bool operator ==(JsGlobalContextRef left, JsGlobalContextRef right)
	{
		return left._handle == right._handle;
	}

	public static bool operator !=(JsGlobalContextRef left, JsGlobalContextRef right)
	{
		return left._handle != right._handle;
	}

	public static implicit operator JsContextRef(JsGlobalContextRef globalContextRef)
	{
		return Methods.BitCast<JsGlobalContextRef, JsContextRef>(globalContextRef);
	}

	public static explicit operator JsGlobalContextRef(JsContextRef contextRef)
	{
		return Methods.BitCast<JsContextRef, JsGlobalContextRef>(contextRef);
	}
}
