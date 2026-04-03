using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using UltralightNet.Enums;

namespace UltralightNet.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct UlGpuState : IEquatable<UlGpuState>
{
	public uint ViewportWidth;

	public uint ViewportHeight;

	public Matrix4x4 Transform;

	private byte _EnableTexturing;

	public bool EnableTexturing
	{
		readonly get => Methods.BitCast<byte, bool>(_EnableTexturing);
		set => _EnableTexturing = Methods.BitCast<bool, byte>(value);
	}

	private byte _EnableBlend;

	public bool EnableBlend
	{
		readonly get => Methods.BitCast<byte, bool>(_EnableBlend);
		set => _EnableBlend = Methods.BitCast<bool, byte>(value);
	}

	private byte _ShaderType;

	public ShaderType ShaderType
	{
		readonly get => Methods.BitCast<byte, ShaderType>(_ShaderType);
		set => _ShaderType = Methods.BitCast<ShaderType, byte>(value);
	}

	public uint RenderBufferId;

	public uint Texture1Id;

	public uint Texture2Id;

	public uint Texture3Id;

	private float scalar_0;
	private float scalar_1;
	private float scalar_2;
	private float scalar_3;
	private float scalar_4;
	private float scalar_5;
	private float scalar_6;
	private float scalar_7;
	public readonly Span<float> Scalar =>
		MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in scalar_0), 8);

	private Vector4 vector_0;
	private Vector4 vector_1;
	private Vector4 vector_2;
	private Vector4 vector_3;
	private Vector4 vector_4;
	private Vector4 vector_5;
	private Vector4 vector_6;
	private Vector4 vector_7;
	public readonly Span<Vector4> Vector =>
		MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in vector_0), 8);

	private byte _clipSize;

	public byte ClipSize
	{
		readonly get => _clipSize;
		set
		{
			static void Throw()
			{
				throw new ArgumentOutOfRangeException(nameof(value), "ClipSize can't be bigger than 8");
			}

			if (value <= 8) _clipSize = value;
			else Throw();
		}
	}

	private Matrix4x4 clip_0;
	private Matrix4x4 clip_1;
	private Matrix4x4 clip_2;
	private Matrix4x4 clip_3;
	private Matrix4x4 clip_4;
	private Matrix4x4 clip_5;
	private Matrix4x4 clip_6;
	private Matrix4x4 clip_7;
	public readonly Span<Matrix4x4> Clip =>
		MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in clip_0), 8).Slice(0, _clipSize);

	private byte _EnableScissor;

	public bool EnableScissor
	{
		readonly get => Methods.BitCast<byte, bool>(_EnableScissor);
		set => _EnableScissor = Methods.BitCast<bool, byte>(value);
	}

	public UlIntRect ScissorRect;

	public readonly bool Equals(UlGpuState other) =>
		#if NETCOREAPP3_0_OR_GREATER
		Vector64.Create(ViewportWidth, ViewportHeight).Equals(Vector64.Create(other.ViewportWidth, other.ViewportHeight))
		#else
		ViewportWidth == other.ViewportWidth && ViewportHeight == other.ViewportHeight
		                                     #endif
		                                     && Transform == other.Transform
		                                     && EnableTexturing == other.EnableTexturing &&
		                                     EnableBlend == other.EnableBlend
		                                     #if NETCOREAPP3_0_OR_GREATER
		&& Vector128.Create(RenderBufferId, Texture1Id, Texture2Id, Texture3Id).Equals(Vector128.Create(other.RenderBufferId, other.Texture1Id, other.Texture2Id, other.Texture3Id))
		&& Vector256.Create(scalar_0, scalar_1, scalar_2, scalar_3, scalar_4, scalar_5, scalar_6, scalar_7).Equals(Vector256.Create(other.scalar_0, other.scalar_1, other.scalar_2, other.scalar_3, other.scalar_4, other.scalar_5, other.scalar_6, other.scalar_7))
		                                     #else
		                                     && RenderBufferId == other.RenderBufferId &&
		                                     Texture1Id == other.Texture1Id && Texture2Id == other.Texture2Id &&
		                                     Texture3Id == other.Texture3Id
		                                     && Scalar.SequenceEqual(other.Scalar)
		                                     #endif
		                                     && Vector.SequenceEqual(other.Vector)
		                                     && ClipSize == other.ClipSize && Clip.SequenceEqual(other.Clip)
		                                     && EnableScissor == other.EnableScissor
		                                     && ScissorRect == other.ScissorRect;

	public readonly override bool Equals(object? other)
	{
		return other is UlGpuState otherState && Equals(otherState);
	}

	public override int GetHashCode()
	{
		var hashCode = new HashCode();
		hashCode.Add(ViewportWidth);
		hashCode.Add(ViewportHeight);
		hashCode.Add(Transform);
		hashCode.Add(_EnableTexturing);
		hashCode.Add(_EnableBlend);
		hashCode.Add(_ShaderType);
		hashCode.Add(RenderBufferId);
		hashCode.Add(Texture1Id);
		hashCode.Add(Texture2Id);
		hashCode.Add(Texture3Id);
		hashCode.Add(scalar_0);
		hashCode.Add(scalar_1);
		hashCode.Add(scalar_2);
		hashCode.Add(scalar_3);
		hashCode.Add(scalar_4);
		hashCode.Add(scalar_5);
		hashCode.Add(scalar_6);
		hashCode.Add(scalar_7);
		hashCode.Add(vector_0);
		hashCode.Add(vector_1);
		hashCode.Add(vector_2);
		hashCode.Add(vector_3);
		hashCode.Add(vector_4);
		hashCode.Add(vector_5);
		hashCode.Add(vector_6);
		hashCode.Add(vector_7);
		hashCode.Add(_clipSize);
		hashCode.Add(clip_0);
		hashCode.Add(clip_1);
		hashCode.Add(clip_2);
		hashCode.Add(clip_3);
		hashCode.Add(clip_4);
		hashCode.Add(clip_5);
		hashCode.Add(clip_6);
		hashCode.Add(clip_7);
		hashCode.Add(_EnableScissor);
		hashCode.Add(ScissorRect);
		return hashCode.ToHashCode();
	}

	public static bool operator ==(UlGpuState left, UlGpuState right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(UlGpuState left, UlGpuState right)
	{
		return !(left == right);
	}
}
