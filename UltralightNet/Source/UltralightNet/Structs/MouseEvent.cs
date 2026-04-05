using System.Runtime.InteropServices;
using UltralightNet.Enums;
using UltralightNet.Structs;

namespace UltralightNet
{
	public static unsafe partial class Methods
	{
		[DllImport(LibUltralight)]
		public static extern MouseEvent* ulCreateMouseEvent(MouseEventType type, int x, int y,
			MouseEventButton button);

		[DllImport(LibUltralight)]
		public static extern void ulDestroyMouseEvent(MouseEvent* evt);
	}
}

namespace UltralightNet.Structs
{
	/// <summary>
	///     Mouse Event
	/// </summary>
	public struct MouseEvent : IEquatable<MouseEvent>
	{
		private int _type;

		public MouseEventType Type
		{
			readonly get => Methods.BitCast<int, MouseEventType>(_type);
			set => _type = Methods.BitCast<MouseEventType, int>(value);
		}

		public int X;
		public int Y;
		private int _button;

		public MouseEventButton Button
		{
			readonly get => Methods.BitCast<int, MouseEventButton>(_button);
			set => _button = Methods.BitCast<MouseEventButton, int>(value);
		}

		public readonly bool Equals(MouseEvent other)
		{
			return Type == other.Type && X == other.X && Y == other.Y && Button == other.Button;
		}

		public override bool Equals(object? obj)
		{
			return obj is MouseEvent other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_type, X, Y, _button);
		}
	}
}
