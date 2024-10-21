using System.Linq;

using Vortice.DirectInput;

using Engine.Interoperation;

namespace Engine.Utilities;

public sealed partial class Input
{
    // Immutable snapshot of the input state
    private static InputSnapshot s_currentSnapshot;

    private static IDirectInput8 s_directInput;
    private static IDirectInputDevice8 s_mouse;
    private static IDirectInputDevice8 s_keyboard;
    private static IDirectInputDevice8 s_joystick;

    private static Vector2 s_axis = Vector2.Zero;
    private static Vector2 s_joystickAxis = Vector2.Zero;

    private static Vector2 s_mouseDelta = Vector2.Zero;
    private static Vector2 s_mousePosition = Vector2.Zero;
    private static Vector2 s_lockedmousePosition = Vector2.Zero;
    private static int s_mouseWheel = 0;
    private static bool s_mouseLockState = false;

    public static void Initialize(IntPtr windowHandle)
    {
        s_directInput = DInput.DirectInput8Create();

        var _mouseGuid = s_directInput.GetDevices(DeviceClass.Pointer, DeviceEnumerationFlags.AttachedOnly).FirstOrDefault()?.InstanceGuid;
        if (_mouseGuid is not null)
        {
            s_mouse = s_directInput.CreateDevice(_mouseGuid.Value);
            s_mouse.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            s_mouse.SetDataFormat<RawMouseState>();
        }

        var _keyboardGuid = s_directInput.GetDevices(DeviceClass.Keyboard, DeviceEnumerationFlags.AttachedOnly).FirstOrDefault()?.InstanceGuid;
        if (_keyboardGuid is not null)
        {
            s_keyboard = s_directInput.CreateDevice(_keyboardGuid.Value);
            s_keyboard.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            s_keyboard.SetDataFormat<RawKeyboardState>();
        }

        var joystickGuid = s_directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly).FirstOrDefault()?.InstanceGuid;
        if (joystickGuid is not null)
        {
            s_joystick = s_directInput.CreateDevice(joystickGuid.Value);
            s_joystick.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            s_joystick.SetDataFormat<RawJoystickState>();
        }
    }

    public static void Fetch()
    {
        s_mouse?.Acquire();
        s_mouse?.Poll();

        s_keyboard?.Acquire();
        s_keyboard?.Poll();

        s_joystick?.Acquire();
        s_joystick?.Poll();
    }

    public static void Dispose()
    {
        s_directInput?.Dispose();
        s_keyboard?.Dispose();
        s_mouse?.Dispose();
        s_joystick?.Dispose();
    }
}

public sealed partial class Input
{
    public static void Update()
    {
        Fetch();

        try
        {
            var currentMouseState = s_mouse?.GetCurrentMouseState();
            if (currentMouseState is not null)
            {
                // Get the pointer position from Win32.
                User32.GetCursorPos(out var pointer);
                s_mousePosition.X = pointer.X;
                s_mousePosition.Y = pointer.Y;

                // Get the pointer delta from the mouseState.
                s_mouseDelta.X = currentMouseState.X;
                s_mouseDelta.Y = currentMouseState.Y;

                s_mouseDelta.Y *= -1; // The DirectX Y Coordinate starts at the top.

                // Get the mouse delta clamped to -1 and 1.
                s_mouseWheel = Math.Clamp(currentMouseState.Z, -1, 1);
            }

            var currentJoystickState = s_joystick?.GetCurrentJoystickState();
            if (currentJoystickState is not null)
            {
                // Reset joystick axis vector.
                s_joystickAxis = Vector2.Zero;
                // Update joystick axis based on input.
                s_joystickAxis.X = currentJoystickState.X / 32767f; // Normalize joystick values to a range of -1 to 1.
                s_joystickAxis.Y = currentJoystickState.Y / 32767f;
            }

            // Create a new snapshot of the input state
            s_currentSnapshot = new()
            {
                MousePosition = s_mousePosition,
                MouseDelta = s_mouseDelta,
                MouseWheel = s_mouseWheel,
                JoystickAxis = s_joystickAxis,
                PreviousMouseState = s_currentSnapshot?.MouseState,
                PreviousKeyboardState = s_currentSnapshot?.KeyboardState,
                PreviousJoystickState = s_currentSnapshot?.JoystickState,
                MouseState = s_mouse?.GetCurrentMouseState(),
                KeyboardState = s_keyboard?.GetCurrentKeyboardState(),
                JoystickState = s_joystick?.GetCurrentJoystickState(),
            };

            // Reset axis vector.
            s_axis = Vector2.Zero;
            // Update axis based on keyboard input.
            if (GetKey(Key.W)) s_axis.Y++;
            if (GetKey(Key.S)) s_axis.Y--;
            if (GetKey(Key.D)) s_axis.X++;
            if (GetKey(Key.A)) s_axis.X--;

            s_currentSnapshot.Axis = s_axis;
        }
        catch { }

        if (s_mouseLockState)
            LockMouse();
        else
            s_lockedmousePosition = s_mousePosition;
    }
}

public sealed partial class Input
{
    public static bool GetKey(Key key, InputState state = InputState.Pressed)
    {
        if (s_currentSnapshot is null)
            return false;

        return state switch
        {
            InputState.Down => s_currentSnapshot.KeyboardState.IsPressed((Vortice.DirectInput.Key)key) && !s_currentSnapshot.PreviousKeyboardState.IsPressed((Vortice.DirectInput.Key)key),
            InputState.Pressed => s_currentSnapshot.KeyboardState.IsPressed((Vortice.DirectInput.Key)key),
            InputState.Up => !s_currentSnapshot.KeyboardState.IsPressed((Vortice.DirectInput.Key)key) && s_currentSnapshot.PreviousKeyboardState.IsPressed((Vortice.DirectInput.Key)key),
            _ => false
        };
    }

    public static bool GetButton(MouseButton button, InputState state = InputState.Pressed)
    {
        if (s_currentSnapshot is null)
            return false;

        return state switch
        {
            InputState.Down => button switch
            {
                MouseButton.Left => s_currentSnapshot.MouseState.Buttons[0] && !s_currentSnapshot.PreviousMouseState.Buttons[0],
                MouseButton.Right => s_currentSnapshot.MouseState.Buttons[1] && !s_currentSnapshot.PreviousMouseState.Buttons[1],
                MouseButton.Middle => s_currentSnapshot.MouseState.Buttons[2] && !s_currentSnapshot.PreviousMouseState.Buttons[2],
                _ => false
            },
            InputState.Pressed => button switch
            {
                MouseButton.Left => s_currentSnapshot.MouseState.Buttons[0],
                MouseButton.Right => s_currentSnapshot.MouseState.Buttons[1],
                MouseButton.Middle => s_currentSnapshot.MouseState.Buttons[2],
                _ => false
            },
            InputState.Up => button switch
            {
                MouseButton.Left => !s_currentSnapshot.MouseState.Buttons[0] && s_currentSnapshot.PreviousMouseState.Buttons[0],
                MouseButton.Right => !s_currentSnapshot.MouseState.Buttons[1] && s_currentSnapshot.PreviousMouseState.Buttons[1],
                MouseButton.Middle => !s_currentSnapshot.MouseState.Buttons[2] && s_currentSnapshot.PreviousMouseState.Buttons[2],
                _ => false
            },
            _ => false
        };
    }

    public static Vector2 GetAxis() =>
        s_currentSnapshot.Axis.IsNaN() ? Vector2.Zero : s_currentSnapshot.Axis;

    public static Vector2 GetJoystickAxis() =>
        s_currentSnapshot.JoystickAxis.IsNaN() ? Vector2.Zero : s_currentSnapshot.JoystickAxis;

    public static Vector2 GetMouseDelta() =>
        s_currentSnapshot.MouseDelta.IsNaN() ? Vector2.Zero : s_currentSnapshot.MouseDelta;

    public static Vector2 GetMousePosition() =>
        s_currentSnapshot.MousePosition.IsNaN() ? Vector2.Zero : s_currentSnapshot.MousePosition;

    public static Vector2 GetRawMousePosition() =>
        s_currentSnapshot?.MousePosition ?? Vector2.Zero;

    public static int GetMouseWheel() =>
        s_currentSnapshot?.MouseWheel ?? 0;
}

public sealed partial class Input
{
    public static void SetCursorIcon(SystemCursor? cursor = null) =>
        User32.SetCursor(User32.LoadCursor(cursor.HasValue ? (nint)cursor.Value : IntPtr.Zero, null));

    public static void SetMouseLockState(bool locked) =>
        s_mouseLockState = locked;

    public static void SetMousePosition(int x, int y) =>
        User32.SetCursorPos((int)s_lockedmousePosition.X, (int)s_lockedmousePosition.Y);

    /// <summary> The values need to be between 0 and 1 </summary>
    public static void SetMouseRelativePosition(Vector3 relativePosition) =>
        SetMouseRelativePosition(relativePosition.X, relativePosition.Y);

    /// <summary> The values need to be between 0 and 1 </summary>
    public static void SetMouseRelativePosition(float u, float v)
    {
        int titlebarHeight = 32;

        User32.GetWindowRect(AppWindow.Win32Window.Handle, out var rect);
        User32.SetCursorPos(
            (int)(AppWindow.Win32Window.Width * u) + rect.Left,
            (int)(AppWindow.Win32Window.Height * v) + rect.Top + titlebarHeight);
    }

    private static void LockMouse()
    {
        User32.SetCursorPos((int)s_lockedmousePosition.X, (int)s_lockedmousePosition.Y);

        if (GetKey(Key.Escape, InputState.Pressed))
            SetMouseLockState(false);
    }
}

public enum MouseButton
{
    Left,
    Right,
    Middle
}

public enum InputState
{
    Down,
    Pressed,
    Up
}

public class InputSnapshot
{
    public Vector2 MousePosition { get; init; }
    public Vector2 MouseDelta { get; init; }
    public int MouseWheel { get; init; }
    public Vector2 Axis { get; set; }
    public Vector2 JoystickAxis { get; init; }

    public MouseState MouseState { get; init; }
    public KeyboardState KeyboardState { get; init; }
    public JoystickState JoystickState { get; init; }
    public MouseState PreviousMouseState { get; init; }
    public KeyboardState PreviousKeyboardState { get; init; }
    public JoystickState PreviousJoystickState { get; init; }
}

public enum Key
{
    Escape = 1,
    D1 = 2,
    D2 = 3,
    D3 = 4,
    D4 = 5,
    D5 = 6,
    D6 = 7,
    D7 = 8,
    D8 = 9,
    D9 = 10,
    D0 = 11,
    Minus = 12,
    Equals = 13,
    Back = 14,
    Tab = 15,
    Q = 16,
    W = 17,
    E = 18,
    R = 19,
    T = 20,
    Y = 21,
    U = 22,
    I = 23,
    O = 24,
    P = 25,
    LeftBracket = 26,
    RightBracket = 27,
    Return = 28,
    LeftControl = 29,
    A = 30,
    S = 31,
    D = 32,
    F = 33,
    G = 34,
    H = 35,
    J = 36,
    K = 37,
    L = 38,
    Semicolon = 39,
    Apostrophe = 40,
    Grave = 41,
    LeftShift = 42,
    Backslash = 43,
    Z = 44,
    X = 45,
    C = 46,
    V = 47,
    B = 48,
    N = 49,
    M = 50,
    Comma = 51,
    Period = 52,
    Slash = 53,
    RightShift = 54,
    Multiply = 55,
    LeftAlt = 56,
    Space = 57,
    Capital = 58,
    F1 = 59,
    F2 = 60,
    F3 = 61,
    F4 = 62,
    F5 = 63,
    F6 = 64,
    F7 = 65,
    F8 = 66,
    F9 = 67,
    F10 = 68,
    NumberLock = 69,
    ScrollLock = 70,
    NumberPad7 = 71,
    NumberPad8 = 72,
    NumberPad9 = 73,
    Subtract = 74,
    NumberPad4 = 75,
    NumberPad5 = 76,
    NumberPad6 = 77,
    Add = 78,
    NumberPad1 = 79,
    NumberPad2 = 80,
    NumberPad3 = 81,
    NumberPad0 = 82,
    Decimal = 83,
    Oem102 = 86,
    F11 = 87,
    F12 = 88,
    F13 = 100,
    F14 = 101,
    F15 = 102,
    Kana = 112,
    AbntC1 = 115,
    Convert = 121,
    NoConvert = 123,
    Yen = 125,
    AbntC2 = 126,
    NumberPadEquals = 141,
    PreviousTrack = 144,
    AT = 145,
    Colon = 146,
    Underline = 147,
    Kanji = 148,
    Stop = 149,
    AX = 150,
    Unlabeled = 151,
    NextTrack = 153,
    NumberPadEnter = 156,
    RightControl = 157,
    Mute = 160,
    Calculator = 161,
    PlayPause = 162,
    MediaStop = 164,
    VolumeDown = 174,
    VolumeUp = 176,
    WebHome = 178,
    NumberPadComma = 179,
    Divide = 181,
    PrintScreen = 183,
    RightAlt = 184,
    Pause = 197,
    Home = 199,
    Up = 200,
    PageUp = 201,
    Left = 203,
    Right = 205,
    End = 207,
    Down = 208,
    PageDown = 209,
    Insert = 210,
    Delete = 211,
    LeftWindowsKey = 219,
    RightWindowsKey = 220,
    Applications = 221,
    Power = 222,
    Sleep = 223,
    Wake = 227,
    WebSearch = 229,
    WebFavorites = 230,
    WebRefresh = 231,
    WebStop = 232,
    WebForward = 233,
    WebBack = 234,
    MyComputer = 235,
    Mail = 236,
    MediaSelect = 237,
    CapsLock = 58,
    Unknown = 0
}