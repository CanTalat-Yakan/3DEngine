// JSBase.h

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace UltralightNet.JavaScript;

public static class JsBase
{
	/// <summary>
	/// Evaluates a string of JavaScript.
	/// </summary>
	/// <param name="context">The execution context to use.</param>
	/// <param name="script">A JSString containing the script to evaluate.</param>
	/// <param name="thisObject">The object to use as "this," or NULL to use the global object as "this."</param>
	/// <param name="sourceUrl">A JSString containing a URL for the script's source file. This is used by debuggers and when reporting exceptions. Pass NULL if you do not care to include source file information.</param>
	/// <param name="startingLineNumber">An integer value specifying the script's starting line number in the file located at sourceURL. This is only used when reporting exceptions. The value is one-based, so the first line is line 1 and invalid values are clamped to 1.</param>
	/// <returns>The JSValue that results from evaluating script, or NULL if an exception is thrown.</returns>
	public static unsafe JsValueRef EvaluateScript(JsContextRef context, JsStringRef script, JsObjectRef thisObject,
		JsStringRef sourceUrl, int startingLineNumber) =>
		JavaScriptMethods.JSEvaluateScript(context, script, thisObject, sourceUrl, startingLineNumber);
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Those are 1:1 definitions")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "Compatibility")]
internal static unsafe partial class JavaScriptMethods
{
	private const string LibWebCore = "WebCore";

	static JavaScriptMethods()
	{
		Methods.Preload();
	}

	internal static Exception UnsupportedMethodException => new NotSupportedException("Method is not supported");

	internal static void ThrowUnsupportedConstructor()
	{
		throw new NotSupportedException("Constructor is not supported");
	}

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSEvaluateScript(JsContextRef context, JsStringRef script, JsObjectRef thisObject,
		JsStringRef sourceURL, int startingLineNumber, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSCheckScriptSyntax(JsContextRef context, JsStringRef script, JsStringRef sourceURL,
		int startingLineNumber, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial void JSGarbageCollect(JsContextRef context);
}

public abstract unsafe class JsNativeContainer<TNativeHandle> : NativeContainer where TNativeHandle : unmanaged
{
	public TNativeHandle JsHandle
	{
		get => Methods.BitCast<nuint, TNativeHandle>((nuint)Handle);
		protected init => Handle = (void*)Methods.BitCast<TNativeHandle, nuint>(value);
	}
}
