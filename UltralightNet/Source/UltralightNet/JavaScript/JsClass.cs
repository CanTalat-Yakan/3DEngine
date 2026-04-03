using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UltralightNet.JavaScript.Structs;

namespace UltralightNet.JavaScript;

public static class JsClass
{
	/// <summary>
	/// Creates a <see cref="JsClassRef">JavaScript class</see> suitable for use with JSObjectMake.
	/// </summary>
	/// <param name="jsClassDefinition">A JsClassDefinition that defines the class.</param>
	/// <returns>A <see cref="JsClassRef">JsClass</see> with the given definition. Ownership follows the Create Rule.</returns>
	public static JsClassRef Create(in JsClassDefinition jsClassDefinition) =>
		JavaScriptMethods.JSClassCreate(in jsClassDefinition);

	/// <summary>
	/// Retains a <see cref="JsClassRef">JavaScript class</see>
	/// </summary>
	/// <param name="jsClass">the class to retain</param>
	/// <returns>A <see cref="JsClassRef">class</see> that is the same as jsClass.</returns>
	public static JsClassRef Retain(this JsClassRef jsClass) =>
		JavaScriptMethods.JSClassRetain(jsClass);

	/// <summary>
	/// Releases a <see cref="JsClassRef">JavaScript class</see>
	/// </summary>
	/// <param name="jsClass">the class to release</param>
	public static void Release(this JsClassRef jsClass) =>
		JavaScriptMethods.JSClassRelease(jsClass);

	/// <summary>
	/// Gets a class's private data.
	/// </summary>
	/// <param name="jsClass">A <see cref="JsClassRef">JsClass</see> whose private data you want to get.</param>
	/// <returns>An <see cref="IntPtr"/> that is the class's private data, if the class has private data, otherwise <see cref="IntPtr.Zero"/>.</returns>
	public static IntPtr GetPrivate(this JsClassRef jsClass) =>
		JavaScriptMethods.JSClassGetPrivate(jsClass);

	/// <summary>
	/// Sets a pointer to private data on a class.
	/// </summary>
	/// <param name="jsClass">The <see cref="JsClassRef">JsClass</see> whose private data you want to set.</param>
	/// <param name="data">An <see cref="IntPtr"/> to set as the class's private data.</param>
	/// <returns>true if class can store private data, otherwise false.</returns>
	public static bool SetPrivate(this JsClassRef jsClass, IntPtr data) =>
		JavaScriptMethods.JSClassSetPrivate(jsClass, data);
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Those are 1:1 definitions")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "Compatibility")]
internal partial class JavaScriptMethods
{
	[LibraryImport(LibWebCore)]
	public static partial JsClassRef JSClassCreate(in JsClassDefinition jsClassDefinition);

	[LibraryImport(LibWebCore)]
	public static partial JsClassRef JSClassRetain(JsClassRef jsClass);

	[LibraryImport(LibWebCore)]
	public static partial void JSClassRelease(JsClassRef jsClass);

	[LibraryImport(LibWebCore)]
	public static partial nint JSClassGetPrivate(JsClassRef jsClass);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSClassSetPrivate(JsClassRef jsClass, nint data);

}

/// <summary>
/// A JavaScript class. Used with JSObjectMake to construct objects with custom behavior.
/// </summary>
public readonly struct JsClassRef
{
	#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
	private readonly nuint _handle;
	#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

	public JsClassRef()
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

	public static bool operator ==(JsClassRef left, JsClassRef right)
	{
		return left._handle == right._handle;
	}

	public static bool operator !=(JsClassRef left, JsClassRef right)
	{
		return left._handle != right._handle;
	}
}
