using System.Runtime.InteropServices;

namespace UltralightNet.JavaScript;

public static class JsContextGroup
{
	public static JsContextGroupRef Create() =>
		JavaScriptMethods.JSContextGroupCreate();

	public static JsContextGroupRef Retain(this JsContextGroupRef contextGroup) =>
		JavaScriptMethods.JSContextGroupRetain(contextGroup);

	public static void Release(this JsContextGroupRef contextGroup) =>
		JavaScriptMethods.JSContextGroupRelease(contextGroup);
}

internal partial class JavaScriptMethods
{
	[LibraryImport(LibWebCore)]
	public static partial JsContextGroupRef JSContextGroupCreate();

	[LibraryImport(LibWebCore)]
	public static partial JsContextGroupRef JSContextGroupRetain(JsContextGroupRef contextGroup);

	[LibraryImport(LibWebCore)]
	public static partial void JSContextGroupRelease(JsContextGroupRef contextGroup);
}

public readonly struct JsContextGroupRef
{
	#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
	private readonly nuint _handle;
	#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

	public JsContextGroupRef()
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

	public static bool operator ==(JsContextGroupRef left, JsContextGroupRef right)
	{
		return left._handle == right._handle;
	}

	public static bool operator !=(JsContextGroupRef left, JsContextGroupRef right)
	{
		return left._handle != right._handle;
	}
}
