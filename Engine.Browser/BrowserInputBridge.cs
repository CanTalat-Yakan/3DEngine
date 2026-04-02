using System.Runtime.InteropServices;
using SDL3;
using UltralightNet;

namespace Engine;

/// <summary>
/// Translates SDL3 input events into Ultralight mouse, keyboard, and scroll events.
/// Attaches to <see cref="AppWindow.SDLEvent"/> for automatic forwarding.
/// </summary>
internal static class BrowserInputBridge
{
    private static readonly ILogger Logger = Log.Category("Engine.Browser.Input");

    /// <summary>
    /// Processes a single SDL event and forwards it to the given <see cref="BrowserInstance"/>.
    /// </summary>
    public static void ProcessEvent(SDL.Event e, BrowserInstance browser)
    {
        switch ((SDL.EventType)e.Type)
        {
            case SDL.EventType.MouseMotion:
                browser.FireMouseEvent(new ULMouseEvent
                {
                    Type = ULMouseEventType.MouseMoved,
                    X = (int)e.Motion.X,
                    Y = (int)e.Motion.Y,
                    Button = ULMouseEventButton.None
                });
                break;

            case SDL.EventType.MouseButtonDown:
                browser.FireMouseEvent(new ULMouseEvent
                {
                    Type = ULMouseEventType.MouseDown,
                    X = (int)e.Button.X,
                    Y = (int)e.Button.Y,
                    Button = MapMouseButton(e.Button.Button)
                });
                break;

            case SDL.EventType.MouseButtonUp:
                browser.FireMouseEvent(new ULMouseEvent
                {
                    Type = ULMouseEventType.MouseUp,
                    X = (int)e.Button.X,
                    Y = (int)e.Button.Y,
                    Button = MapMouseButton(e.Button.Button)
                });
                break;

            case SDL.EventType.MouseWheel:
            {
                var scroll = new ULScrollEvent
                {
                    Type = ULScrollEventType.ByPixel,
                    DeltaX = (int)(e.Wheel.X * 32),
                    DeltaY = (int)(e.Wheel.Y * 32),
                };
                browser.FireScrollEvent(scroll);
                break;
            }

            case SDL.EventType.KeyDown:
                FireKeyEvent(e, ULKeyEventType.RawKeyDown, browser);
                break;

            case SDL.EventType.KeyUp:
                FireKeyEvent(e, ULKeyEventType.KeyUp, browser);
                break;

            case SDL.EventType.TextInput:
            {
                if (e.Text.Text != IntPtr.Zero)
                {
                    var text = Marshal.PtrToStringUTF8(e.Text.Text);
                    if (!string.IsNullOrEmpty(text))
                    {
                        var charEvt = ULKeyEvent.Create(
                            ULKeyEventType.Char,
                            (ULKeyEventModifiers)0,
                            virtualKeyCode: 0,
                            nativeKeyCode: 0,
                            text: text,
                            unmodifiedText: text,
                            isKeypad: false,
                            isAutoRepeat: false,
                            isSystemKey: false);
                        browser.FireKeyEvent(charEvt);
                    }
                }
                break;
            }
        }
    }

    private static void FireKeyEvent(SDL.Event e, ULKeyEventType type, BrowserInstance browser)
    {
        var scancode = (SDL.Scancode)e.Key.Scancode;
        int vk = MapSdlScancodeToVirtualKey(scancode);
        if (vk == 0) return;

        var modifiers = GetModifiers(e);
        var text = type == ULKeyEventType.RawKeyDown ? "" : "";
        
        var keyEvt = ULKeyEvent.Create(
            type,
            modifiers,
            virtualKeyCode: vk,
            nativeKeyCode: (int)scancode,
            text: text,
            unmodifiedText: text,
            isKeypad: IsKeypad(scancode),
            isAutoRepeat: e.Key.Repeat,
            isSystemKey: false);

        browser.FireKeyEvent(keyEvt);
    }

    private static ULKeyEventModifiers GetModifiers(SDL.Event e)
    {
        var mod = (SDL.Keymod)e.Key.Mod;
        var result = (ULKeyEventModifiers)0;

        if ((mod & SDL.Keymod.Shift) != 0) result |= ULKeyEventModifiers.ShiftKey;
        if ((mod & SDL.Keymod.Ctrl) != 0) result |= ULKeyEventModifiers.CtrlKey;
        if ((mod & SDL.Keymod.Alt) != 0) result |= ULKeyEventModifiers.AltKey;
        if ((mod & SDL.Keymod.GUI) != 0) result |= ULKeyEventModifiers.MetaKey;

        return result;
    }

    private static ULMouseEventButton MapMouseButton(byte sdlButton) => sdlButton switch
    {
        1 => ULMouseEventButton.Left,
        2 => ULMouseEventButton.Middle,
        3 => ULMouseEventButton.Right,
        _ => ULMouseEventButton.None
    };

    private static bool IsKeypad(SDL.Scancode code) => code is
        >= SDL.Scancode.KpDivide and <= SDL.Scancode.KpEquals;

    /// <summary>
    /// Maps SDL3 scancodes to Windows Virtual Key codes (used by Ultralight's ULKeyCodes).
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
        SDL.Scancode.F1 => ULKeyCodes.GK_F1,
        SDL.Scancode.F2 => ULKeyCodes.GK_F2,
        SDL.Scancode.F3 => ULKeyCodes.GK_F3,
        SDL.Scancode.F4 => ULKeyCodes.GK_F4,
        SDL.Scancode.F5 => ULKeyCodes.GK_F5,
        SDL.Scancode.F6 => ULKeyCodes.GK_F6,
        SDL.Scancode.F7 => ULKeyCodes.GK_F7,
        SDL.Scancode.F8 => ULKeyCodes.GK_F8,
        SDL.Scancode.F9 => ULKeyCodes.GK_F9,
        SDL.Scancode.F10 => ULKeyCodes.GK_F10,
        SDL.Scancode.F11 => ULKeyCodes.GK_F11,
        SDL.Scancode.F12 => ULKeyCodes.GK_F12,

        // Navigation
        SDL.Scancode.Return => ULKeyCodes.GK_RETURN,
        SDL.Scancode.Escape => ULKeyCodes.GK_ESCAPE,
        SDL.Scancode.Backspace => ULKeyCodes.GK_BACK,
        SDL.Scancode.Tab => ULKeyCodes.GK_TAB,
        SDL.Scancode.Space => ULKeyCodes.GK_SPACE,
        SDL.Scancode.Delete => ULKeyCodes.GK_DELETE,
        SDL.Scancode.Insert => ULKeyCodes.GK_INSERT,
        SDL.Scancode.Home => ULKeyCodes.GK_HOME,
        SDL.Scancode.End => ULKeyCodes.GK_END,
        SDL.Scancode.Pageup => ULKeyCodes.GK_PRIOR,
        SDL.Scancode.Pagedown => ULKeyCodes.GK_NEXT,

        // Arrow keys
        SDL.Scancode.Left => ULKeyCodes.GK_LEFT,
        SDL.Scancode.Right => ULKeyCodes.GK_RIGHT,
        SDL.Scancode.Up => ULKeyCodes.GK_UP,
        SDL.Scancode.Down => ULKeyCodes.GK_DOWN,

        // Modifiers
        SDL.Scancode.LShift => ULKeyCodes.GK_SHIFT,
        SDL.Scancode.RShift => ULKeyCodes.GK_SHIFT,
        SDL.Scancode.LCtrl => ULKeyCodes.GK_CONTROL,
        SDL.Scancode.RCtrl => ULKeyCodes.GK_CONTROL,
        SDL.Scancode.LAlt => ULKeyCodes.GK_MENU,
        SDL.Scancode.RAlt => ULKeyCodes.GK_MENU,

        // Punctuation
        SDL.Scancode.Minus => ULKeyCodes.GK_OEM_MINUS,
        SDL.Scancode.Equals => ULKeyCodes.GK_OEM_PLUS,
        SDL.Scancode.Leftbracket => ULKeyCodes.GK_OEM_4,
        SDL.Scancode.Rightbracket => ULKeyCodes.GK_OEM_6,
        SDL.Scancode.Backslash => ULKeyCodes.GK_OEM_5,
        SDL.Scancode.Semicolon => ULKeyCodes.GK_OEM_1,
        SDL.Scancode.Apostrophe => ULKeyCodes.GK_OEM_7,
        SDL.Scancode.Comma => ULKeyCodes.GK_OEM_COMMA,
        SDL.Scancode.Period => ULKeyCodes.GK_OEM_PERIOD,
        SDL.Scancode.Slash => ULKeyCodes.GK_OEM_2,
        SDL.Scancode.Grave => ULKeyCodes.GK_OEM_3,

        // Numpad
        SDL.Scancode.Kp0 => ULKeyCodes.GK_NUMPAD0,
        SDL.Scancode.Kp1 => ULKeyCodes.GK_NUMPAD1,
        SDL.Scancode.Kp2 => ULKeyCodes.GK_NUMPAD2,
        SDL.Scancode.Kp3 => ULKeyCodes.GK_NUMPAD3,
        SDL.Scancode.Kp4 => ULKeyCodes.GK_NUMPAD4,
        SDL.Scancode.Kp5 => ULKeyCodes.GK_NUMPAD5,
        SDL.Scancode.Kp6 => ULKeyCodes.GK_NUMPAD6,
        SDL.Scancode.Kp7 => ULKeyCodes.GK_NUMPAD7,
        SDL.Scancode.Kp8 => ULKeyCodes.GK_NUMPAD8,
        SDL.Scancode.Kp9 => ULKeyCodes.GK_NUMPAD9,

        _ => 0 // unmapped
    };
}






