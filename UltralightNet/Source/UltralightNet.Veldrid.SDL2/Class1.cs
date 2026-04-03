using UltralightNet.Enums;
using Veldrid;

namespace UltralightNet.Veldrid.SDL2;

public static class KeyConverter
{
	public static KeyEvent ToUlKeyEvent(in this global::Veldrid.KeyEvent k)
	{
		var type = k.Down ? KeyEventType.RawKeyDown : KeyEventType.KeyUp;
		KeyEventModifiers modifiers = 0;
		int keycode;
		var text = "";

		if ((k.Modifiers & ModifierKeys.Alt) is ModifierKeys.Alt) modifiers |= KeyEventModifiers.AltKey;
		if ((k.Modifiers & ModifierKeys.Control) is ModifierKeys.Control) modifiers |= KeyEventModifiers.CtrlKey;
		if ((k.Modifiers & ModifierKeys.Shift) is ModifierKeys.Shift) modifiers |= KeyEventModifiers.ShiftKey;
		if ((k.Modifiers & ModifierKeys.Gui) is ModifierKeys.Gui) modifiers |= KeyEventModifiers.MetaKey;

		var key = k.Key;

		if (key is Key.KeypadEnter or Key.Enter)
			/*type = KeyEventType.Char;
			if (type is KeyEventType.RawKeyDown)
			{
				text = '\r'.ToString();
			}*/
			keycode = 13;
		else
			keycode = SdLtoUl(key);

		if (k.Down && false)
			if (key > (Key)82 && key < (Key)119)
			{
				//type = KeyEventType.Char;
				if (key < (Key)109)
					text = (modifiers & KeyEventModifiers.ShiftKey) is not 0
						? key.ToString()
						: key.ToString().ToLower();
				else
					text = ((int)key - 109).ToString();
			}

		return KeyEvent.Create(type, modifiers, keycode, 0, text, text, false, k.Repeat, false);
	}

	public static int SdLtoUl(Key key)
	{
		#region 0 - 9

		if (key > (Key)108 && key < (Key)119) return (int)key - 61;

		#endregion

		#region A - Z

		if (key > (Key)82 && key < (Key)109) return (int)key - 18;

		#endregion

		#region CTRL - Shift

		if (key > 0 && key < (Key)5) return (int)key + 159;

		#endregion

		#region F1 - F24

		if (key > (Key)9)
		{
			if (key < (Key)34) return (int)key + 102;
			// F25 - F35 not supported
			if (key < (Key)45) return 0;
		}

		#endregion

		#region Numpad 0 - Numpad 9

		if (key > (Key)66 && key < (Key)77) return (int)key + 29;

		#endregion

		int key_code = key switch
		{
			Key.NumLock => UlKeyCodes.GK_NUMLOCK,
			Key.KeypadDivide => UlKeyCodes.GK_DIVIDE,
			Key.KeypadMultiply => UlKeyCodes.GK_MULTIPLY,
			Key.KeypadSubtract => UlKeyCodes.GK_SUBTRACT,
			Key.KeypadAdd => UlKeyCodes.GK_ADD,
			Key.KeypadDecimal => UlKeyCodes.GK_DECIMAL,

			Key.Escape => UlKeyCodes.GK_ESCAPE,
			Key.Tilde => UlKeyCodes.GK_OEM_3,
			Key.Tab => UlKeyCodes.GK_TAB,
			Key.CapsLock => UlKeyCodes.GK_CAPITAL,
			Key.AltLeft or Key.AltRight => UlKeyCodes.GK_MENU,
			Key.Space => UlKeyCodes.GK_SPACE,

			Key.Slash => UlKeyCodes.GK_OEM_2,


			Key.PrintScreen => UlKeyCodes.GK_SNAPSHOT,
			Key.ScrollLock => UlKeyCodes.GK_SCROLL,

			Key.Insert => UlKeyCodes.GK_INSERT,
			Key.Delete => UlKeyCodes.GK_DELETE,
			Key.Home => UlKeyCodes.GK_HOME,
			Key.End => UlKeyCodes.GK_END,
			Key.PageUp => UlKeyCodes.GK_PRIOR,
			Key.PageDown => UlKeyCodes.GK_NEXT,

			Key.Left => UlKeyCodes.GK_LEFT,
			Key.Right => UlKeyCodes.GK_RIGHT,
			Key.Up => UlKeyCodes.GK_UP,
			Key.Down => UlKeyCodes.GK_DOWN,


			_ => (int)key + 29
		};

		return key_code;
	}
}
