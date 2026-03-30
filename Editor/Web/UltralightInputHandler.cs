using SDL3;
using UltralightNet;

namespace Engine;

/// <summary>
/// Translates SDL events into Ultralight input events (mouse, keyboard, scroll).
/// Hook this into AppWindow.SDLEvent to forward input to the Ultralight view.
/// </summary>
public static class UltralightInputHandler
{
    private static readonly ILogger Logger = Log.Category("Editor.Ultralight.Input");

    /// <summary>Processes an SDL event and forwards it to the Ultralight view.</summary>
    public static void ProcessEvent(SDL.Event e, View view)
    {
        switch ((SDL.EventType)e.Type)
        {
            case SDL.EventType.MouseMotion:
                {
                    var mouseEvent = new ULMouseEvent
                    {
                        Type = ULMouseEventType.MouseMoved,
                        X = (int)e.Motion.X,
                        Y = (int)e.Motion.Y,
                        Button = ULMouseEventButton.None
                    };
                    view.FireMouseEvent(mouseEvent);
                    break;
                }
            case SDL.EventType.MouseButtonDown:
            case SDL.EventType.MouseButtonUp:
                {
                    var isDown = (SDL.EventType)e.Type == SDL.EventType.MouseButtonDown;
                    var button = e.Button.Button switch
                    {
                        1 => ULMouseEventButton.Left,
                        2 => ULMouseEventButton.Middle,
                        3 => ULMouseEventButton.Right,
                        _ => ULMouseEventButton.None
                    };

                    var mouseEvent = new ULMouseEvent
                    {
                        Type = isDown ? ULMouseEventType.MouseDown : ULMouseEventType.MouseUp,
                        X = (int)e.Button.X,
                        Y = (int)e.Button.Y,
                        Button = button
                    };
                    view.FireMouseEvent(mouseEvent);
                    Logger.Trace($"Mouse {(isDown ? "down" : "up")}: button={button} pos=({(int)e.Button.X},{(int)e.Button.Y})");
                    break;
                }
            case SDL.EventType.MouseWheel:
                {
                    var scrollEvent = new ULScrollEvent
                    {
                        Type = ULScrollEventType.ByPixel,
                        DeltaX = (int)(e.Wheel.X * 32),
                        DeltaY = (int)(e.Wheel.Y * 32),
                    };
                    view.FireScrollEvent(scrollEvent);
                    Logger.Trace($"Scroll: dx={scrollEvent.DeltaX} dy={scrollEvent.DeltaY}");
                    break;
                }
            case SDL.EventType.KeyDown:
            case SDL.EventType.KeyUp:
                {
                    var isDown = (SDL.EventType)e.Type == SDL.EventType.KeyDown;
                    var keyType = isDown ? ULKeyEventType.RawKeyDown : ULKeyEventType.KeyUp;
                    var vk = SdlScancodeToUlVirtualKey((uint)e.Key.Scancode);

                    var keyEvent = ULKeyEvent.Create(
                        keyType,
                        modifiers: 0,
                        virtualKeyCode: vk,
                        nativeKeyCode: (int)e.Key.Scancode,
                        text: string.Empty,
                        unmodifiedText: string.Empty,
                        isKeypad: false,
                        isAutoRepeat: e.Key.Repeat,
                        isSystemKey: false);
                    view.FireKeyEvent(keyEvent);
                    keyEvent.Dispose();
                    Logger.Trace($"Key {(isDown ? "down" : "up")}: scancode={e.Key.Scancode} vk={vk} repeat={e.Key.Repeat}");
                    break;
                }
            case SDL.EventType.TextInput:
                {
                    if (e.Text.Text != IntPtr.Zero)
                    {
                        var text = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(e.Text.Text);
                        if (!string.IsNullOrEmpty(text))
                        {
                            var charEvent = ULKeyEvent.Create(
                                ULKeyEventType.Char,
                                modifiers: 0,
                                virtualKeyCode: 0,
                                nativeKeyCode: 0,
                                text: text,
                                unmodifiedText: text,
                                isKeypad: false,
                                isAutoRepeat: false,
                                isSystemKey: false);
                            view.FireKeyEvent(charEvent);
                            charEvent.Dispose();
                            Logger.Trace($"Text input: \"{text}\"");
                        }
                    }
                    break;
                }
        }
    }

    /// <summary>Maps common SDL scancodes to Ultralight/Windows virtual key codes.</summary>
    private static int SdlScancodeToUlVirtualKey(uint scancode) => scancode switch
    {
        // Letters A-Z (SDL_SCANCODE_A=4 .. Z=29)
        >= 4 and <= 29 => 'A' + ((int)scancode - 4),
        // Digits 1-9, 0 (SDL_SCANCODE_1=30..39)
        >= 30 and <= 38 => '1' + ((int)scancode - 30),
        39 => '0',
        // Return, Escape, Backspace, Tab, Space
        40 => ULKeyCodes.GK_RETURN,
        41 => ULKeyCodes.GK_ESCAPE,
        42 => ULKeyCodes.GK_BACK,
        43 => ULKeyCodes.GK_TAB,
        44 => ULKeyCodes.GK_SPACE,
        // Arrow keys
        79 => ULKeyCodes.GK_RIGHT,
        80 => ULKeyCodes.GK_LEFT,
        81 => ULKeyCodes.GK_DOWN,
        82 => ULKeyCodes.GK_UP,
        // F1–F12
        58 => ULKeyCodes.GK_F1,
        59 => ULKeyCodes.GK_F2,
        60 => ULKeyCodes.GK_F3,
        61 => ULKeyCodes.GK_F4,
        62 => ULKeyCodes.GK_F5,
        63 => ULKeyCodes.GK_F6,
        64 => ULKeyCodes.GK_F7,
        65 => ULKeyCodes.GK_F8,
        66 => ULKeyCodes.GK_F9,
        67 => ULKeyCodes.GK_F10,
        68 => ULKeyCodes.GK_F11,
        69 => ULKeyCodes.GK_F12,
        // Home, End, PageUp, PageDown, Delete, Insert
        74 => ULKeyCodes.GK_HOME,
        75 => ULKeyCodes.GK_PRIOR,
        76 => ULKeyCodes.GK_DELETE,
        77 => ULKeyCodes.GK_END,
        78 => ULKeyCodes.GK_NEXT,
        73 => ULKeyCodes.GK_INSERT,
        // Modifiers
        224 => ULKeyCodes.GK_CONTROL,  // Left Ctrl
        225 => ULKeyCodes.GK_SHIFT,    // Left Shift
        226 => ULKeyCodes.GK_MENU,     // Left Alt
        228 => ULKeyCodes.GK_CONTROL,  // Right Ctrl
        229 => ULKeyCodes.GK_SHIFT,    // Right Shift
        230 => ULKeyCodes.GK_MENU,     // Right Alt
        _ => 0
    };
}

