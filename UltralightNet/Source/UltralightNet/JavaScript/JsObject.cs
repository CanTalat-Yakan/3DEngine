// JSObjectRef.h

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UltralightNet.JavaScript.Structs;

namespace UltralightNet.JavaScript;

public static unsafe partial class JsObject
{
	/// <summary>
	/// Gets a object's private data.
	/// </summary>
	/// <param name="jsObject">A <see cref="JsObjectRef">JsObject</see> whose private data you want to get.</param>
	/// <returns>An <see cref="IntPtr"/> that is the object's private data, if the object has private data, otherwise <see cref="IntPtr.Zero"/>.</returns>
	public static IntPtr GetPrivate(this JsObjectRef jsObject) =>
		JavaScriptMethods.JSObjectGetPrivate(jsObject);

	/// <summary>
	/// Sets a pointer to private data on an object.
	/// </summary>
	/// <param name="jsObject">The <see cref="JsObjectRef">JsObject</see> whose private data you want to set.</param>
	/// <param name="data">An <see cref="IntPtr"/> to set as the object's private data.</param>
	/// <returns>true if object can store private data, otherwise false.</returns>
	/// <remarks>
	/// The default object class does not allocate storage for private data. Only objects created with a non-NULL JSClass can store private data.
	/// </remarks>
	public static bool SetPrivate(this JsObjectRef jsObject, IntPtr data) =>
		JavaScriptMethods.JSObjectSetPrivate(jsObject, data);
	public static JsObjectRef Make(JsContextRef context, JsClassRef jsClass, IntPtr privateData = 0) =>
		JavaScriptMethods.JSObjectMake(context, jsClass, privateData);

	public static JsObjectRef MakeFunctionWithCallback(JsContextRef context, JsStringRef name, delegate* unmanaged[Cdecl]<JsContextRef /*ctx*/, JsObjectRef /*function*/, JsObjectRef /*thisObject*/
		, nuint /*argumentCount*/, JsValueRef* /*arguments[]*/, JsValueRef* /*exception*/, JsValueRef> func) =>
		JavaScriptMethods.JSObjectMakeFunctionWithCallback(context, name, func);

	public static JsObjectRef MakeArray(JsContextRef context, nuint length, JsValueRef* elements) =>
		JavaScriptMethods.JSObjectMakeArray(context, length, elements);

	public static JsObjectRef MakeDate(JsContextRef context, nuint argumentCount, JsValueRef* arguments) =>
		JavaScriptMethods.JSObjectMakeDate(context, argumentCount, arguments);

	public static JsObjectRef MakeError(JsContextRef context, nuint argumentCount, JsValueRef* arguments) =>
		JavaScriptMethods.JSObjectMakeError(context, argumentCount, arguments);

	public static JsObjectRef MakeRegExp(JsContextRef context, nuint argumentCount, JsValueRef* arguments) =>
		JavaScriptMethods.JSObjectMakeRegExp(context, argumentCount, arguments);

	public static JsObjectRef MakeDeferredPromise(JsContextRef context, JsObjectRef* resolve, JsObjectRef* reject) =>
		JavaScriptMethods.JSObjectMakeDeferredPromise(context, resolve, reject);

	public static JsObjectRef MakeFunction(JsContextRef context, JsStringRef name, uint parameterCount, JsStringRef* parameterNames,
		JsStringRef body, JsStringRef sourceUrl, int startingLineNumber = 0) =>
		JavaScriptMethods.JSObjectMakeFunction(context, name, parameterCount, parameterNames, body, sourceUrl,
			startingLineNumber);

	public static JsObjectRef MakeFunction(JsContextRef context, JsStringRef name,
		JsStringRef body, JsStringRef sourceUrl, int startingLineNumber = 0) =>
		JavaScriptMethods.JSObjectMakeFunction(context, name, 0, null, body, sourceUrl,
			startingLineNumber);

	public static JsValueRef GetPrototype(this JsObjectRef jsObject, JsContextRef context) =>
		JavaScriptMethods.JSObjectGetPrototype(context, jsObject);

	public static void SetPrototype(this JsObjectRef jsObject, JsContextRef context, JsValueRef jsValue) =>
		JavaScriptMethods.JSObjectSetPrototype(context, jsObject, jsValue);

	public static bool HasProperty(this JsObjectRef jsObject, JsContextRef context, JsStringRef propertyName) =>
		JavaScriptMethods.JSObjectHasProperty(context, jsObject, propertyName);

	public static JsValueRef GetProperty(this JsObjectRef jsObject, JsContextRef context, JsStringRef propertyName) =>
		JavaScriptMethods.JSObjectGetProperty(context, jsObject, propertyName);

	public static void SetProperty(this JsObjectRef jsObject, JsContextRef context, JsStringRef propertyName,
		JsValueRef value, JsPropertyAttributes attributes = JsPropertyAttributes.None) =>
		JavaScriptMethods.JSObjectSetProperty(context, jsObject, propertyName, value, attributes);

	public static bool DeleteProperty(this JsObjectRef jsObject, JsContextRef context, JsStringRef propertyName) =>
		JavaScriptMethods.JSObjectDeleteProperty(context, jsObject, propertyName);

	public static bool HasPropertyForKey(this JsObjectRef jsObject, JsContextRef context, JsValueRef propertyKey) =>
		JavaScriptMethods.JSObjectHasPropertyForKey(context, jsObject, propertyKey);

	public static JsValueRef GetPropertyForKey(this JsObjectRef jsObject, JsContextRef context, JsValueRef propertyKey) =>
		JavaScriptMethods.JSObjectGetPropertyForKey(context, jsObject, propertyKey);

	public static void SetPropertyForKey(this JsObjectRef jsObject, JsContextRef context, JsValueRef propertyKey,
		JsValueRef value) =>
		JavaScriptMethods.JSObjectSetPropertyForKey(context, jsObject, propertyKey, value);

	public static bool DeletePropertyForKey(this JsObjectRef jsObject, JsContextRef context, JsValueRef propertyKey) =>
		JavaScriptMethods.JSObjectDeletePropertyForKey(context, jsObject, propertyKey);

	public static JsValueRef GetPropertyAtIndex(this JsObjectRef jsObject, JsContextRef context, uint index) =>
		JavaScriptMethods.JSObjectGetPropertyAtIndex(context, jsObject, index);

	public static void SetPropertyAtIndex(this JsObjectRef jsObject, JsContextRef context, uint index, JsValueRef value) =>
		JavaScriptMethods.JSObjectSetPropertyAtIndex(context, jsObject, index, value);

	public static bool IsFunction(this JsObjectRef jsObject, JsContextRef context) =>
		JavaScriptMethods.JSObjectIsFunction(context, jsObject);

	public static JsValueRef CallAsFunction(this JsObjectRef jsObject, JsContextRef context, JsObjectRef thisObject,
		UIntPtr argCount, JsValueRef* args) =>
		JavaScriptMethods.JSObjectCallAsFunction(context, jsObject, thisObject, argCount, args);

	public static JsValueRef CallAsFunction(this JsObjectRef jsObject, JsContextRef context, JsObjectRef thisObject) =>
		JavaScriptMethods.JSObjectCallAsFunction(context, jsObject, thisObject, 0, null);

	public static bool IsConstructor(this JsObjectRef jsObject, JsContextRef context) =>
		JavaScriptMethods.JSObjectIsConstructor(context, jsObject);

	public static JsObjectRef CallAsConstructor(this JsObjectRef jsObject, JsContextRef context, UIntPtr argCount,
		JsValueRef* args) =>
		JavaScriptMethods.JSObjectCallAsConstructor(context, jsObject, argCount, args);

	public static JsObjectRef CallAsConstructor(this JsObjectRef jsObject, JsContextRef context) =>
		JavaScriptMethods.JSObjectCallAsConstructor(context, jsObject, 0, null);

	public static JsPropertyNameArrayRef CopyPropertyNames(this JsObjectRef jsObject, JsContextRef context) =>
		JavaScriptMethods.JSObjectCopyPropertyNames(context, jsObject);

	public static bool SetPrivateProperty(this JsObjectRef jsObject, JsContextRef context, JsStringRef propertyName, JsValueRef value) =>
		JavaScriptMethods.JSObjectSetPrivateProperty(context, jsObject, propertyName, value);

	public static JsValueRef GetPrivateProperty(this JsObjectRef jsObject, JsContextRef context, JsStringRef propertyName) =>
		JavaScriptMethods.JSObjectGetPrivateProperty(context, jsObject, propertyName);

	public static bool DeletePrivateProperty(this JsObjectRef jsObject, JsContextRef context, JsStringRef propertyName) =>
		JavaScriptMethods.JSObjectDeletePrivateProperty(context, jsObject, propertyName);

	public static JsObjectRef GetProxyTarget(this JsObjectRef jsObject) =>
		JavaScriptMethods.JSObjectGetProxyTarget(jsObject);

	public static JsGlobalContextRef GetGlobalContext(this JsObjectRef jsObject) =>
		JavaScriptMethods.JSObjectGetGlobalContext(jsObject);
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Those are 1:1 definitions")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "Compatibility")]
internal unsafe partial class JavaScriptMethods
{
	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMake(JsContextRef ctx, JsClassRef jsClass, nint privateData);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMakeFunctionWithCallback(JsContextRef ctx, JsStringRef name,
		delegate* unmanaged[Cdecl]<JsContextRef /*ctx*/, JsObjectRef /*function*/, JsObjectRef /*thisObject*/
			, nuint /*argumentCount*/, JsValueRef* /*arguments[]*/, JsValueRef* /*exception*/, JsValueRef> func);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMakeConstructor(JsContextRef ctx, JsClassRef jsClass,
		delegate* unmanaged[Cdecl]<JsContextRef /*ctx*/, JsObjectRef /*constructor*/, nuint /*argumentCount*/
			, JsValueRef* /*arguments[]*/, JsValueRef* /*exception*/, JsObjectRef> func);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMakeArray(JsContextRef ctx, nuint argumentCount, JsValueRef* arguments,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMakeDate(JsContextRef ctx, nuint argumentCount, JsValueRef* arguments,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMakeError(JsContextRef ctx, nuint argumentCount, JsValueRef* arguments,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMakeRegExp(JsContextRef ctx, nuint argumentCount, JsValueRef* arguments,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMakeDeferredPromise(JsContextRef ctx, JsObjectRef* resolve,
		JsObjectRef* reject, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMakeFunction(JsContextRef ctx, JsStringRef name, uint parameterCount,
		JsStringRef* parameterNames, JsStringRef body, JsStringRef sourceURL, int startingLineNumber,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSObjectGetPrototype(JsContextRef ctx, JsObjectRef jsObject);

	[LibraryImport(LibWebCore)]
	public static partial void JSObjectSetPrototype(JsContextRef ctx, JsObjectRef jsObject, JsValueRef jsValue);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSObjectHasProperty(JsContextRef ctx, JsObjectRef jsObject, JsStringRef propertyName);

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSObjectGetProperty(JsContextRef ctx, JsObjectRef jsObject,
		JsStringRef propertyName, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial void JSObjectSetProperty(JsContextRef ctx, JsObjectRef jsObject, JsStringRef propertyName,
		JsValueRef value, JsPropertyAttributes attributes = JsPropertyAttributes.None, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSObjectDeleteProperty(JsContextRef ctx, JsObjectRef jsObject, JsStringRef propertyName,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSObjectHasPropertyForKey(JsContextRef ctx, JsObjectRef jsObject, JsValueRef propertyKey,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSObjectGetPropertyForKey(JsContextRef ctx, JsObjectRef jsObject,
		JsValueRef propertyKey, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial void JSObjectSetPropertyForKey(JsContextRef ctx, JsObjectRef jsObject, JsValueRef propertyKey,
		JsValueRef value, JsPropertyAttributes attributes = JsPropertyAttributes.None, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSObjectDeletePropertyForKey(JsContextRef ctx, JsObjectRef jsObject,
		JsValueRef propertyKey, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSObjectGetPropertyAtIndex(JsContextRef ctx, JsObjectRef jsObject,
		uint propertyIndex, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial void JSObjectSetPropertyAtIndex(JsContextRef ctx, JsObjectRef jsObject, uint propertyIndex,
		JsValueRef value, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial nint JSObjectGetPrivate(JsObjectRef jsObject);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSObjectSetPrivate(JsObjectRef jsObject, nint data);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSObjectIsFunction(JsContextRef ctx, JsObjectRef jsObject);

	[LibraryImport(LibWebCore)]
	public static partial JsValueRef JSObjectCallAsFunction(JsContextRef ctx, JsObjectRef jsObject,
		JsObjectRef thisObject, nuint argumentCount, JsValueRef* arguments, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static partial bool JSObjectIsConstructor(JsContextRef ctx, JsObjectRef jsObject);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectCallAsConstructor(JsContextRef ctx, JsObjectRef jsObject,
		nuint argumentCount, JsValueRef* arguments, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsPropertyNameArrayRef JSObjectCopyPropertyNames(JsContextRef ctx, JsObjectRef jsObject);
}

public readonly struct JsObjectRef
{
	#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
	private readonly nuint _handle;
	#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

	public JsObjectRef()
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

	public static bool operator ==(JsObjectRef left, JsObjectRef right)
	{
		return left._handle == right._handle;
	}

	public static bool operator !=(JsObjectRef left, JsObjectRef right)
	{
		return left._handle != right._handle;
	}

	public static implicit operator JsValueRef(JsObjectRef jsObject)
	{
		return Methods.BitCast<JsObjectRef, JsValueRef>(jsObject);
	}

	public static explicit operator JsObjectRef(JsValueRef jsValue)
	{
		return Methods.BitCast<JsValueRef, JsObjectRef>(jsValue);
	}
}

/// <summary>
/// A set of JsPropertyAttributes. Combine multiple attributes by logically ORing them together.
/// </summary>
[Flags]
public enum JsPropertyAttributes : uint
{
	None = 0,
	ReadOnly = 1 << 1,
	DontEnum = 1 << 2,
	DontDelete = 1 << 3
}

/// <summary>
/// A set of JsClassAttributes. Combine multiple attributes by logically ORing them together.
/// </summary>
[Flags]
public enum JsClassAttributes : uint
{
	None = 0,
	NoAutomaticPrototype = 1 << 1
}
