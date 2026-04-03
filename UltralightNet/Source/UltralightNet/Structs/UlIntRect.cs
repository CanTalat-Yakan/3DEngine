#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace UltralightNet.Structs;

public struct UlIntRect : IEquatable<UlIntRect>
{
	public int Left;
	public int Top;
	public int Right;
	public int Bottom;

	public readonly bool IsEmpty => Left == Right || Top == Bottom;

	public readonly bool Equals(UlIntRect other) =>
		#if NETCOREAPP3_0_OR_GREATER
		Vector128.Create(Left, Top, Right, Bottom).Equals(Vector128.Create(other.Left, other.Top, other.Right, other.Bottom));
		#else
		Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
	#endif
	public readonly override bool Equals(object? other)
	{
		return other is UlIntRect rect && Equals(rect);
	}

	public static bool operator ==(UlIntRect? left, UlIntRect? right)
	{
		return left is not null ? right is not null && left.Equals(right) : right is null;
	}

	public static bool operator !=(UlIntRect? left, UlIntRect? right)
	{
		return !(left == right);
	}

	#if NETSTANDARD2_1 || NETCOREAPP2_1_OR_GREATER
	public readonly override int GetHashCode()
	{
		return HashCode.Combine(Left, Top, Right, Bottom);
	}
	#endif

	public static explicit operator UlIntRect(UlRect rect)
		#if NET7_0_OR_GREATER
	{
		Vector128<float> float4 = Vector128.Create(rect.Left, rect.Top, rect.Right, rect.Bottom);
		Vector128<int> int4 = Vector128.ConvertToInt32(float4); // thx Tanner Gooding and TrumpMcDonaldz
		return System.Runtime.CompilerServices.Unsafe.As<Vector128<int>, UlIntRect>(ref int4); // thx rickbrew
	}
		#else
		=> new() { Left = (int)rect.Left, Top = (int)rect.Top, Right = (int)rect.Right, Bottom = (int)rect.Bottom };
	#endif
}
