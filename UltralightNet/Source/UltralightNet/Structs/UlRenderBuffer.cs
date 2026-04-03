namespace UltralightNet.Structs;

public struct UlRenderBuffer
{
	public uint TextureId;
	public uint Width;
	public uint Height;

	private byte _hasStencilBuffer;

	public bool HasStencilBuffer
	{
		readonly get => Methods.BitCast<byte, bool>(_hasStencilBuffer);
		set => _hasStencilBuffer = Methods.BitCast<bool, byte>(value);
	}

	private byte _hasDepthBuffer;

	public bool HasDepthBuffer
	{
		readonly get => Methods.BitCast<byte, bool>(_hasDepthBuffer);
		set => _hasDepthBuffer = Methods.BitCast<bool, byte>(value);
	}
}
