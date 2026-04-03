using UltralightNet.Enums;

namespace UltralightNet.Structs;

public unsafe ref struct UlVertexBuffer
{
	private byte _format;
	public uint Size;
	public void* Data;

	public VertexBufferFormat Format
	{
		readonly get => Methods.BitCast<byte, VertexBufferFormat>(_format);
		set => _format = Methods.BitCast<VertexBufferFormat, byte>(value);
	}
}
