#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace UltralightNet.Structs;

public struct UlRect : IEquatable<UlRect>
{
	public float Left;
	public float Top;
	public float Right;
	public float Bottom;

	public readonly bool IsEmpty => Math.Abs(Left - Right) < float.Epsilon || Math.Abs(Top - Bottom) < float.Epsilon;

	public readonly bool Equals(UlRect other) =>
		#if NETCOREAPP3_0_OR_GREATER
		Vector128.Create(Left, Top, Right, Bottom).Equals(Vector128.Create(other.Left, other.Top, other.Right, other.Bottom));
		#else
		Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
	#endif
	public readonly override bool Equals(object? other)
	{
		return other is UlRect rect && Equals(rect);
	}

	public static bool operator ==(UlRect? left, UlRect? right)
	{
		return left is not null ? right is not null && left.Equals(right) : right is null;
	}

	public static bool operator !=(UlRect? left, UlRect? right)
	{
		return !(left == right);
	}

	#if NETSTANDARD2_1 || NETCOREAPP2_1_OR_GREATER
	public readonly override int GetHashCode()
	{
		return HashCode.Combine(Left, Top, Right, Bottom);
	}
	#endif

	public static explicit operator UlRect(UlIntRect rect)
		#if NET7_0_OR_GREATER
	{
		Vector128<int> int4 = Vector128.Create(rect.Left, rect.Top, rect.Right, rect.Bottom);
		Vector128<float> float4 = Vector128.ConvertToSingle(int4); // thx Tanner Gooding and TrumpMcDonaldz
		return System.Runtime.CompilerServices.Unsafe.As<Vector128<float>, UlRect>(ref float4); // thx rickbrew
	}
		#else
		=> new() { Left = rect.Left, Top = rect.Top, Right = rect.Right, Bottom = rect.Bottom };
	#endif
}
