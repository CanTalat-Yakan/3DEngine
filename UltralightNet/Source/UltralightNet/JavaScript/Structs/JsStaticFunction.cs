namespace UltralightNet.JavaScript.Structs;

public unsafe struct JsStaticFunction
{
	public byte* Name;
	public delegate* unmanaged[Cdecl]<void*, void*, void*, nuint, void**, void**, void*> CallAsFunction;
	public JsPropertyAttributes Attributes;
}

public unsafe struct JsStaticFunctionEx
{
	public byte* Name;
	public delegate* unmanaged[Cdecl]<void*, void*, void*, void*, void*, nuint, void**, void**, void*> CallAsFunctionEx;
	public JsPropertyAttributes Attributes;
}
