using UltralightNet.Enums;
using UltralightNet.Platform.HighPerformance;

namespace UltralightNet.Structs;

/// <summary>Rendering details for a View, to be used with your own GPUDriver</summary>
public struct RenderTarget : IEquatable<RenderTarget>
{
	private byte _isEmpty;

	/// <summary>Whether this target is empty (null texture)</summary>
	public bool IsEmpty
	{
		readonly get => Methods.BitCast<byte, bool>(_isEmpty);
		set => _isEmpty = Methods.BitCast<bool, byte>(value);
	}

	/// <summary>The viewport width (in device coordinates).</summary>
	public uint Width;

	/// <summary>The viewport height (in device coordinates).</summary>
	public uint Height;

	/// <summary><see cref="GpuDriver" />'s texture id</summary>
	public uint TextureId;

	/// <summary>The texture width (in pixels). This may be padded.</summary>
	public uint TextureWidth;

	/// <summary>The texture height (in pixels). This may be padded.</summary>
	public uint TextureHeight;

	private byte _textureFormat;

	/// <summary>The pixel format of the texture.</summary>
	public BitmapFormat TextureFormat
	{
		readonly get => Methods.BitCast<byte, BitmapFormat>(_textureFormat);
		set => _textureFormat = Methods.BitCast<BitmapFormat, byte>(value);
	}

	/// <summary>UV coordinates of the texture (this is needed because the texture may be padded).</summary>
	public UlRect Uv;

	/// <summary><see cref="GpuDriver" />'s render buffer id</summary>
	public uint RenderBufferId;

	public readonly bool Equals(RenderTarget rt)
	{
		return IsEmpty == rt.IsEmpty &&
		       Width == rt.Width &&
		       Height == rt.Height &&
		       TextureId == rt.TextureId &&
		       TextureWidth == rt.TextureWidth &&
		       TextureHeight == rt.TextureHeight &&
		       TextureFormat == rt.TextureFormat &&
		       Uv == rt.Uv &&
		       RenderBufferId == rt.RenderBufferId;
	}

	public readonly override bool Equals(object? other)
	{
		return other is RenderTarget rt && Equals(rt);
	}
	#if NETSTANDARD2_1 || NETCOREAPP2_1_OR_GREATER
	public readonly override int GetHashCode()
	{
		return HashCode.Combine(HashCode.Combine(IsEmpty, Width, Height, TextureId),
			HashCode.Combine(TextureWidth, TextureHeight, TextureFormat, Uv), RenderBufferId);
	}
	#endif
}
