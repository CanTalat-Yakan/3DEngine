// JSTypedArray.h

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace UltralightNet.JavaScript;

public static unsafe partial class JsObject
{
	public static JsObjectRef MakeTypedArray(JsContextRef context, JsTypedArrayType arrayType, UIntPtr length) =>
		JavaScriptMethods.JSObjectMakeTypedArray(context, arrayType, length);

	public static JsObjectRef MakeTypedArrayWithBytesNoCopy(JsContextRef context, JsTypedArrayType arrayType, void* bytes, UIntPtr byteCount, delegate* unmanaged[Cdecl]<void*, nint, void> /*JSTypedArrayBytesDeallocator*/ bytesDeallocator,
		IntPtr deallocatorContext) =>
		JavaScriptMethods.JSObjectMakeTypedArrayWithBytesNoCopy(context, arrayType, bytes, byteCount, bytesDeallocator, deallocatorContext);

	public static JsObjectRef MakeTypedArrayWithArrayBuffer(JsContextRef context, JsTypedArrayType arrayType, JsObjectRef buffer) =>
		JavaScriptMethods.JSObjectMakeTypedArrayWithArrayBuffer(context, arrayType, buffer);

	public static JsObjectRef MakeTypedArrayWithArrayBuffer(JsContextRef context, JsTypedArrayType arrayType, JsObjectRef buffer, UIntPtr byteOffset, UIntPtr length) =>
		JavaScriptMethods.JSObjectMakeTypedArrayWithArrayBufferAndOffset(context, arrayType, buffer, byteOffset, length);

	public static void* GetTypedArrayBytesPtr(JsContextRef context, JsObjectRef jsObject) =>
		JavaScriptMethods.JSObjectGetTypedArrayBytesPtr(context, jsObject);
	public static UIntPtr GetTypedArrayLength(JsContextRef context, JsObjectRef jsObject) =>
		JavaScriptMethods.JSObjectGetTypedArrayLength(context, jsObject);

	public static UIntPtr GetTypedArrayByteLength(JsContextRef context, JsObjectRef jsObject) =>
		JavaScriptMethods.JSObjectGetTypedArrayByteLength(context, jsObject);

	public static UIntPtr GetTypedArrayByteOffset(JsContextRef context, JsObjectRef jsObject) =>
		JavaScriptMethods.JSObjectGetTypedArrayByteOffset(context, jsObject);

	public static JsObjectRef GetTypedArrayBuffer(JsContextRef context, JsObjectRef jsObject) =>
		JavaScriptMethods.JSObjectGetTypedArrayBuffer(context, jsObject);

	public static JsObjectRef MakeArrayBufferWithBytesNoCopy(JsContextRef context, void* bytes, UIntPtr byteCount, delegate* unmanaged[Cdecl]<void*, nint, void> /*JSTypedArrayBytesDeallocator*/ bytesDeallocator,
		IntPtr deallocatorContext) =>
		JavaScriptMethods.JSObjectMakeArrayBufferWithBytesNoCopy(context, bytes, byteCount, bytesDeallocator, deallocatorContext);

	public static void* GetArrayBufferBytesPtr(JsContextRef context, JsObjectRef jsObject) =>
		JavaScriptMethods.JSObjectGetArrayBufferBytesPtr(context, jsObject);

	public static UIntPtr GetArrayBufferByteLength(JsContextRef context, JsObjectRef jsObject) =>
		JavaScriptMethods.JSObjectGetArrayBufferByteLength(context, jsObject);
}

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Those are 1:1 definitions")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", Justification = "Compatibility")]
internal unsafe partial class JavaScriptMethods
{
	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMakeTypedArray(JsContextRef ctx, JsTypedArrayType arrayType,
		nuint length, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMakeTypedArrayWithBytesNoCopy(JsContextRef ctx,
		JsTypedArrayType arrayType, void* bytes, nuint byteLength,
		delegate* unmanaged[Cdecl]<void*, nint, void> /*JSTypedArrayBytesDeallocator*/ bytesDeallocator,
		nint deallocatorContext, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMakeTypedArrayWithArrayBuffer(JsContextRef ctx,
		JsTypedArrayType arrayType, JsObjectRef buffer, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMakeTypedArrayWithArrayBufferAndOffset(JsContextRef ctx,
		JsTypedArrayType arrayType, JsObjectRef buffer, nuint byteOffset, nuint length,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial void* JSObjectGetTypedArrayBytesPtr(JsContextRef ctx, JsObjectRef jsObject,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial nuint JSObjectGetTypedArrayLength(JsContextRef ctx, JsObjectRef jsObject,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial nuint JSObjectGetTypedArrayByteLength(JsContextRef ctx, JsObjectRef jsObject,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial nuint JSObjectGetTypedArrayByteOffset(JsContextRef ctx, JsObjectRef jsObject,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectGetTypedArrayBuffer(JsContextRef ctx, JsObjectRef jsObject,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial JsObjectRef JSObjectMakeArrayBufferWithBytesNoCopy(JsContextRef ctx, void* bytes,
		nuint byteLength,
		delegate* unmanaged[Cdecl]<void*, nint, void> /*JSTypedArrayBytesDeallocator*/ bytesDeallocator,
		nint deallocatorContext, JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial void* JSObjectGetArrayBufferBytesPtr(JsContextRef ctx, JsObjectRef jsObject,
		JsValueRef* exception = null);

	[LibraryImport(LibWebCore)]
	public static partial nuint JSObjectGetArrayBufferByteLength(JsContextRef ctx, JsObjectRef jsObject,
		JsValueRef* exception = null);
}
