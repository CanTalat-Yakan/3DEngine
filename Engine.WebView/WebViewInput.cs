using System.Runtime.InteropServices;
using SDL3;
using UltralightNet;
using UltralightNet.Enums;
using UltralightNet.Structs;

namespace Engine;

/// <summary>
/// Translates SDL3 input events into Ultralight mouse, keyboard, and scroll events.
/// Attaches to <see cref="AppWindow.SDLEvent"/> for automatic forwarding.
/// </summary>
/// <remarks>
/// Handles mouse motion, button press/release, scroll wheel, key down/up, and text input.
/// Key events are mapped from <see cref="SDL.Scancode"/> to Windows Virtual Key codes
/// via <see cref="MapSdlScancodeToVirtualKey"/> for Ultralight's internal key handling.
/// </remarks>
/// <seealso cref="WebViewInstance"/>
/// <seealso cref="WebViewPlugin"/>
internal static class WebViewInput
{
    private static readonly ILogger Logger = Log.Category("Engine.WebView.Input");

    /// <summary>
    /// Processes a single SDL event and forwards it to the given <see cref="WebViewInstance"/>.
    /// </summary>
    /// <param name="e">The raw SDL event to translate.</param>
    /// <param name="webView">The Ultralight view instance to forward input to.</param>
    public static void ProcessEvent(SDL.Event e, WebViewInstance webView)
    {
        switch ((SDL.EventType)e.Type)
        {
            case SDL.EventType.MouseMotion:
                webView.FireMouseEvent(new MouseEvent
                {
                    Type = MouseEventType.MouseMoved,
                    X = (int)e.Motion.X,
                    Y = (int)e.Motion.Y,
                    Button = MouseEventButton.None
                });
                break;

            case SDL.EventType.MouseButtonDown:
                webView.FireMouseEvent(new MouseEvent
                {
                    Type = MouseEventType.MouseDown,
                    X = (int)e.Button.X,
                    Y = (int)e.Button.Y,
                    Button = MapMouseButton(e.Button.Button)
                });
                break;

            case SDL.EventType.MouseButtonUp:
                webView.FireMouseEvent(new MouseEvent
                {
                    Type = MouseEventType.MouseUp,
                    X = (int)e.Button.X,
                    Y = (int)e.Button.Y,
                    Button = MapMouseButton(e.Button.Button)
                });
                break;

            case SDL.EventType.MouseWheel:
            {
                var scroll = new ScrollEvent
                {
                    Type = ScrollEventType.ByPixel,
                    DeltaX = (int)(e.Wheel.X * 32),
                    DeltaY = (int)(e.Wheel.Y * 32),
                };
                webView.FireScrollEvent(scroll);
                break;
            }

            case SDL.EventType.KeyDown:
                FireKeyEvent(e, KeyEventType.RawKeyDown, webView);
                break;

            case SDL.EventType.KeyUp:
                FireKeyEvent(e, KeyEventType.KeyUp, webView);
                break;

            case SDL.EventType.TextInput:
            {
                if (e.Text.Text != IntPtr.Zero)
                {
                    var text = Marshal.PtrToStringUTF8(e.Text.Text);
                    if (!string.IsNullOrEmpty(text))
                    {
                        var charEvt = KeyEvent.Create(
                            KeyEventType.Char,
                            (KeyEventModifiers)0,
                            virtualKeyCode: 0,
                            nativeKeyCode: 0,
                            text: text,
                            unmodifiedText: text,
                            isKeypad: false,
                            isAutoRepeat: false,
                            isSystemKey: false);
                        webView.FireKeyEvent(charEvt);
                    }
                }
                break;
            }
        }
    }

    /// <summary>Builds and fires a key event (RawKeyDown or KeyUp) into the webview.</summary>
    /// <param name="e">The SDL key event.</param>
    /// <param name="type">The Ultralight key event type to emit.</param>
    /// <param name="webView">The target webview instance.</param>
    private static void FireKeyEvent(SDL.Event e, KeyEventType type, WebViewInstance webView)
    {
        var scancode = (SDL.Scancode)e.Key.Scancode;
        int vk = MapSdlScancodeToVirtualKey(scancode);
        if (vk == 0) return;

        var modifiers = GetModifiers(e);
        var text = type == KeyEventType.RawKeyDown ? "" : "";
        
        var keyEvt = KeyEvent.Create(
            type,
            modifiers,
            virtualKeyCode: vk,
            nativeKeyCode: (int)scancode,
            text: text,
            unmodifiedText: text,
            isKeypad: IsKeypad(scancode),
            isAutoRepeat: e.Key.Repeat,
            isSystemKey: false);

        webView.FireKeyEvent(keyEvt);
    }

    /// <summary>Extracts keyboard modifier flags from an SDL key event.</summary>
    /// <param name="e">The SDL event containing key modifier state.</param>
    /// <returns>The corresponding Ultralight <see cref="KeyEventModifiers"/> flags.</returns>
    private static KeyEventModifiers GetModifiers(SDL.Event e)
    {
        var mod = (SDL.Keymod)e.Key.Mod;
        var result = (KeyEventModifiers)0;

        if ((mod & SDL.Keymod.Shift) != 0) result |= KeyEventModifiers.ShiftKey;
        if ((mod & SDL.Keymod.Ctrl) != 0) result |= KeyEventModifiers.CtrlKey;
        if ((mod & SDL.Keymod.Alt) != 0) result |= KeyEventModifiers.AltKey;
        if ((mod & SDL.Keymod.GUI) != 0) result |= KeyEventModifiers.MetaKey;

        return result;
    }

    /// <summary>Maps an SDL mouse button index to an Ultralight <see cref="MouseEventButton"/>.</summary>
    /// <param name="sdlButton">The SDL button index (1=left, 2=middle, 3=right).</param>
    /// <returns>The corresponding <see cref="MouseEventButton"/>, or <see cref="MouseEventButton.None"/> for unknown buttons.</returns>
    private static MouseEventButton MapMouseButton(byte sdlButton) => sdlButton switch
    {
        1 => MouseEventButton.Left,
        2 => MouseEventButton.Middle,
        3 => MouseEventButton.Right,
        _ => MouseEventButton.None
    };

    /// <summary>Checks whether an SDL scancode falls in the numeric keypad range.</summary>
    /// <param name="code">The SDL scancode to test.</param>
    /// <returns><c>true</c> if the scancode is a keypad key.</returns>
    private static bool IsKeypad(SDL.Scancode code) => code is
        >= SDL.Scancode.KpDivide and <= SDL.Scancode.KpEquals;

    /// <summary>
    /// Maps SDL3 scancodes to Windows Virtual Key codes (used by Ultralight's UlKeyCodes).
    /// This covers common keys; extend as needed.
    /// </summary>
    private static int MapSdlScancodeToVirtualKey(SDL.Scancode scancode) => scancode switch
    {
        // Letters A-Z → VK 0x41 – 0x5A
        SDL.Scancode.A => 0x41,
        SDL.Scancode.B => 0x42,
        SDL.Scancode.C => 0x43,
        SDL.Scancode.D => 0x44,
        SDL.Scancode.E => 0x45,
        SDL.Scancode.F => 0x46,
        SDL.Scancode.G => 0x47,
        SDL.Scancode.H => 0x48,
        SDL.Scancode.I => 0x49,
        SDL.Scancode.J => 0x4A,
        SDL.Scancode.K => 0x4B,
        SDL.Scancode.L => 0x4C,
        SDL.Scancode.M => 0x4D,
        SDL.Scancode.N => 0x4E,
        SDL.Scancode.O => 0x4F,
        SDL.Scancode.P => 0x50,
        SDL.Scancode.Q => 0x51,
        SDL.Scancode.R => 0x52,
        SDL.Scancode.S => 0x53,
        SDL.Scancode.T => 0x54,
        SDL.Scancode.U => 0x55,
        SDL.Scancode.V => 0x56,
        SDL.Scancode.W => 0x57,
        SDL.Scancode.X => 0x58,
        SDL.Scancode.Y => 0x59,
        SDL.Scancode.Z => 0x5A,

        // Digits 0-9 → VK 0x30 – 0x39
        SDL.Scancode.Alpha0 => 0x30,
        SDL.Scancode.Alpha1 => 0x31,
        SDL.Scancode.Alpha2 => 0x32,
        SDL.Scancode.Alpha3 => 0x33,
        SDL.Scancode.Alpha4 => 0x34,
        SDL.Scancode.Alpha5 => 0x35,
        SDL.Scancode.Alpha6 => 0x36,
        SDL.Scancode.Alpha7 => 0x37,
        SDL.Scancode.Alpha8 => 0x38,
        SDL.Scancode.Alpha9 => 0x39,

        // Function keys F1-F12
        SDL.Scancode.F1 => UlKeyCodes.GK_F1,
        SDL.Scancode.F2 => UlKeyCodes.GK_F2,
        SDL.Scancode.F3 => UlKeyCodes.GK_F3,
        SDL.Scancode.F4 => UlKeyCodes.GK_F4,
        SDL.Scancode.F5 => UlKeyCodes.GK_F5,
        SDL.Scancode.F6 => UlKeyCodes.GK_F6,
        SDL.Scancode.F7 => UlKeyCodes.GK_F7,
        SDL.Scancode.F8 => UlKeyCodes.GK_F8,
        SDL.Scancode.F9 => UlKeyCodes.GK_F9,
        SDL.Scancode.F10 => UlKeyCodes.GK_F10,
        SDL.Scancode.F11 => UlKeyCodes.GK_F11,
        SDL.Scancode.F12 => UlKeyCodes.GK_F12,

        // Navigation
        SDL.Scancode.Return => UlKeyCodes.GK_RETURN,
        SDL.Scancode.Escape => UlKeyCodes.GK_ESCAPE,
        SDL.Scancode.Backspace => UlKeyCodes.GK_BACK,
        SDL.Scancode.Tab => UlKeyCodes.GK_TAB,
        SDL.Scancode.Space => UlKeyCodes.GK_SPACE,
        SDL.Scancode.Delete => UlKeyCodes.GK_DELETE,
        SDL.Scancode.Insert => UlKeyCodes.GK_INSERT,
        SDL.Scancode.Home => UlKeyCodes.GK_HOME,
        SDL.Scancode.End => UlKeyCodes.GK_END,
        SDL.Scancode.Pageup => UlKeyCodes.GK_PRIOR,
        SDL.Scancode.Pagedown => UlKeyCodes.GK_NEXT,

        // Arrow keys
        SDL.Scancode.Left => UlKeyCodes.GK_LEFT,
        SDL.Scancode.Right => UlKeyCodes.GK_RIGHT,
        SDL.Scancode.Up => UlKeyCodes.GK_UP,
        SDL.Scancode.Down => UlKeyCodes.GK_DOWN,

        // Modifiers
        SDL.Scancode.LShift => UlKeyCodes.GK_SHIFT,
        SDL.Scancode.RShift => UlKeyCodes.GK_SHIFT,
        SDL.Scancode.LCtrl => UlKeyCodes.GK_CONTROL,
        SDL.Scancode.RCtrl => UlKeyCodes.GK_CONTROL,
        SDL.Scancode.LAlt => UlKeyCodes.GK_MENU,
        SDL.Scancode.RAlt => UlKeyCodes.GK_MENU,

        // Punctuation
        SDL.Scancode.Minus => UlKeyCodes.GK_OEM_MINUS,
        SDL.Scancode.Equals => UlKeyCodes.GK_OEM_PLUS,
        SDL.Scancode.Leftbracket => UlKeyCodes.GK_OEM_4,
        SDL.Scancode.Rightbracket => UlKeyCodes.GK_OEM_6,
        SDL.Scancode.Backslash => UlKeyCodes.GK_OEM_5,
        SDL.Scancode.Semicolon => UlKeyCodes.GK_OEM_1,
        SDL.Scancode.Apostrophe => UlKeyCodes.GK_OEM_7,
        SDL.Scancode.Comma => UlKeyCodes.GK_OEM_COMMA,
        SDL.Scancode.Period => UlKeyCodes.GK_OEM_PERIOD,
        SDL.Scancode.Slash => UlKeyCodes.GK_OEM_2,
        SDL.Scancode.Grave => UlKeyCodes.GK_OEM_3,

        // Numpad
        SDL.Scancode.Kp0 => UlKeyCodes.GK_NUMPAD0,
        SDL.Scancode.Kp1 => UlKeyCodes.GK_NUMPAD1,
        SDL.Scancode.Kp2 => UlKeyCodes.GK_NUMPAD2,
        SDL.Scancode.Kp3 => UlKeyCodes.GK_NUMPAD3,
        SDL.Scancode.Kp4 => UlKeyCodes.GK_NUMPAD4,
        SDL.Scancode.Kp5 => UlKeyCodes.GK_NUMPAD5,
        SDL.Scancode.Kp6 => UlKeyCodes.GK_NUMPAD6,
        SDL.Scancode.Kp7 => UlKeyCodes.GK_NUMPAD7,
        SDL.Scancode.Kp8 => UlKeyCodes.GK_NUMPAD8,
        SDL.Scancode.Kp9 => UlKeyCodes.GK_NUMPAD9,

        _ => 0 // unmapped
    };
}

