// JSObjectRefPrivate.h

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace UltralightNet.JavaScript;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal partial class JavaScriptMethods
{
	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSObjectSetPrivateProperty(JsContextRef ctx, JsObjectRef jsObject,
		JsStringRef propertyName, JsValueRef value);

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSObjectGetPrivateProperty(JsContextRef ctx, JsObjectRef jsObject,
		JsStringRef propertyName);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSObjectDeletePrivateProperty(JsContextRef ctx, JsObjectRef jsObject,
		JsStringRef propertyName);

	/// <summary>
	///     TODO: may not work
	/// </summary>
	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectGetProxyTarget(JsObjectRef jsObject);

	[LibraryImport(LibWebCore)]
	public static partial JsGlobalContextRef JSObjectGetGlobalContext(JsObjectRef jsObject);
}
