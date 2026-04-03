namespace UltralightNet.JavaScript.Structs;

public unsafe struct JsStaticValue
{
	public byte* Name;
	public delegate* unmanaged[Cdecl]<void*, void*, void*, void**, void*> GetProperty;
	public delegate* unmanaged[Cdecl]<void*, void*, void*, void*, void**, bool> SetProperty;
	private uint _attributes;

	public JsPropertyAttributes Attributes
	{
		readonly get => Methods.BitCast<uint, JsPropertyAttributes>(_attributes);
		set => _attributes = Methods.BitCast<JsPropertyAttributes, uint>(value);
	}
}

public unsafe struct JsStaticValueEx
{
	public byte* Name;
	public delegate* unmanaged[Cdecl]<void*, void*, void*, void*, void**, void*> GetPropertyEx;
	public delegate* unmanaged[Cdecl]<void*, void*, void*, void*, void*, void**, bool> SetPropertyEx;
	private uint _attributes;

	public JsPropertyAttributes Attributes
	{
		readonly get => Methods.BitCast<uint, JsPropertyAttributes>(_attributes);
		set => _attributes = Methods.BitCast<JsPropertyAttributes, uint>(value);
	}
}
