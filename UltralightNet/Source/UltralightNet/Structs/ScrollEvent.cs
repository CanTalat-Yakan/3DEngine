using UltralightNet.Enums;

namespace UltralightNet.Structs;

/// <summary>
///     Scroll event
/// </summary>
public struct ScrollEvent : IEquatable<ScrollEvent>
{
	private int _type;

	/// <summary>
	///     Type of event
	/// </summary>
	public ScrollEventType Type
	{
		readonly get => Methods.BitCast<int, ScrollEventType>(_type);
		set => _type = Methods.BitCast<ScrollEventType, int>(value);
	}

	/// <summary>
	///     horizontal scroll
	/// </summary>
	public int DeltaX;

	/// <summary>
	///     vertical scroll
	/// </summary>
	public int DeltaY;

	public readonly bool Equals(ScrollEvent other)
	{
		return Type == other.Type && DeltaX == other.DeltaX && DeltaY == other.DeltaY;
	}

	public override bool Equals(object? obj)
	{
		return obj is ScrollEvent @event && Equals(@event);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(_type, DeltaX, DeltaY);
	}
}
