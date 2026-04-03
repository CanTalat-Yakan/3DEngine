namespace UltralightNet.Enums;

/// <summary>An enumeration of the different keyboard modifiers.</summary>
[Flags]
public enum KeyEventModifiers : byte // KeyEvent.h - uint8_t, CAPI_KeyEvent.h - unsigned int
{
	/// <summary>Whether an ALT key is down</summary>
	AltKey = 1 << 0,

	/// <summary>Whether a Control key is down</summary>
	CtrlKey = 1 << 1,

	/// <summary>Whether a meta key (Command key on Mac, Windows key on Windows) is down</summary>
	MetaKey = 1 << 2,

	/// <summary>Whether a Shift key is down</summary>
	ShiftKey = 1 << 3
}
