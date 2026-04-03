using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace UltralightNet.JavaScript;

public static class JsPropertyNameAccumulator
{
	public static void AddName(this JsPropertyNameAccumulatorRef accumulator, JsStringRef propertyName) =>
		JavaScriptMethods.JSPropertyNameAccumulatorAddName(accumulator, propertyName);
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Those are 1:1 definitions")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "Compatibility")]
internal partial class JavaScriptMethods
{
	[LibraryImport(LibWebCore)]
	public static partial void JSPropertyNameAccumulatorAddName(JsPropertyNameAccumulatorRef accumulator,
		JsStringRef propertyName);
}

public readonly struct JsPropertyNameAccumulatorRef
{
	#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
	private readonly nuint _handle;
	#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

	public JsPropertyNameAccumulatorRef()
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

	public static bool operator ==(JsPropertyNameAccumulatorRef left, JsPropertyNameAccumulatorRef right)
	{
		return left._handle == right._handle;
	}

	public static bool operator !=(JsPropertyNameAccumulatorRef left, JsPropertyNameAccumulatorRef right)
	{
		return left._handle != right._handle;
	}
}
