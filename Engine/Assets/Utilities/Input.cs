global using Key = Vortice.DirectInput.Key;

using System.Linq;

using Vortice.DirectInput;

namespace Engine.Utilities;

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

public sealed class Input
{
    private static IDirectInput8 s_directInput;
    private static IDirectInputDevice8 s_mouse;
    private static IDirectInputDevice8 s_keyboard;
    private static IDirectInputDevice8 s_joystick;

    private static MouseState s_mouseState = new();
    private static KeyboardState s_keyboardState = new();
    private static JoystickState s_joystickState = new();

    private static Vector2 s_axis = Vector2.Zero;
    private static Vector2 s_joystickAxis = Vector2.Zero;

    private static Vector2 s_mouseDelta = Vector2.Zero;
    private static Vector2 s_mousePosition = Vector2.Zero;
    private static int s_mouseWheel = 0;
    private static bool s_lockMouse;

    public static void Initialize(IntPtr windowHandle)
    {
        s_directInput = DInput.DirectInput8Create();

        var _mouseGuid = s_directInput.GetDevices(DeviceClass.Pointer, DeviceEnumerationFlags.AttachedOnly).FirstOrDefault()?.InstanceGuid;
        if (_mouseGuid != null)
        {
            s_mouse = s_directInput.CreateDevice(_mouseGuid.Value);
            s_mouse.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            s_mouse.SetDataFormat<RawMouseState>();
        }

        var _keyboardGuid = s_directInput.GetDevices(DeviceClass.Keyboard, DeviceEnumerationFlags.AttachedOnly).FirstOrDefault()?.InstanceGuid;
        if (_keyboardGuid != null)
        {
            s_keyboard = s_directInput.CreateDevice(_keyboardGuid.Value);
            s_keyboard.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            s_keyboard.SetDataFormat<RawKeyboardState>();
        }

        var joystickGuid = s_directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly).FirstOrDefault()?.InstanceGuid;
        if (joystickGuid != null)
        {
            s_joystick = s_directInput.CreateDevice(joystickGuid.Value);
            s_joystick.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            s_joystick.SetDataFormat<RawJoystickState>();
        }
    }

    public static void Dispose()
    {
        s_directInput?.Dispose();
        s_keyboard?.Dispose();
        s_mouse?.Dispose();
        s_joystick?.Dispose();
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

    public static void Update()
    {
        try
        {
            var currentMouseState = s_mouse?.GetCurrentMouseState();
            if (currentMouseState is not null)
            {
                // Get the mouse position.
                s_mousePosition.X = currentMouseState.X;
                s_mousePosition.Y = currentMouseState.Y;

                // Calculate mouse axis based on the difference between the current and previous pointer positions.
                s_mouseDelta.X = s_mousePosition.X - currentMouseState.X;
                s_mouseDelta.Y = s_mousePosition.Y - currentMouseState.Y;

                s_mouseDelta.Y *= -1; // The DirectX Y Coord starts at the top.

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
        }
        catch { }

        if (s_lockMouse)
            Vortice.Win32.User32.SetCursorPos(0, 0);

        // Reset axis vector.
        s_axis = Vector2.Zero;
        // Update axis based on keyboard input.
        if (GetKey(Key.W)) s_axis.Y++;
        if (GetKey(Key.S)) s_axis.Y--;
        if (GetKey(Key.D)) s_axis.X++;
        if (GetKey(Key.A)) s_axis.X--;
    }

    public static void FixedUpdate()
    {
        try
        {
            var currentMouseState = s_mouse?.GetCurrentMouseState();
            if (currentMouseState is not null)
            {
                // Calculate mouse axis based on the difference between the current and previous pointer positions.
                s_mouseDelta.X = s_mousePosition.X - currentMouseState.X;
                s_mouseDelta.Y = s_mousePosition.Y - currentMouseState.Y;

                s_mouseDelta.Y *= -1; // The DirectX Y Coord starts at the top.
            }
        }
        catch { }
    }

    public static void LateUpdate()
    {
        try
        {
            s_mouseState = s_mouse?.GetCurrentMouseState();
            s_keyboardState = s_keyboard?.GetCurrentKeyboardState();
            s_joystickState = s_joystick?.GetCurrentJoystickState();
        }
        catch { }
    }

    public static bool GetKey(Key key, InputState state = InputState.Pressed)
    {
        KeyboardState currentKeyboardState = null;
        try { currentKeyboardState = s_keyboard?.GetCurrentKeyboardState(); }
        catch { return false; }

        return state switch
        {
            InputState.Down => currentKeyboardState.IsPressed(key) && !s_keyboardState.IsPressed(key),
            InputState.Pressed => currentKeyboardState.IsPressed(key),
            InputState.Up => !currentKeyboardState.IsPressed(key) && s_keyboardState.IsPressed(key),
            _ => false
        };
    }

    public static bool GetButton(MouseButton button, InputState state = InputState.Pressed)
    {
        MouseState currentMouseState = null;
        try { currentMouseState = s_mouse?.GetCurrentMouseState(); }
        catch { return false; }

        return state switch
        {
            InputState.Down => button switch
            {
                MouseButton.Left => currentMouseState.Buttons[0] && !s_mouseState.Buttons[0],
                MouseButton.Right => currentMouseState.Buttons[1] && !s_mouseState.Buttons[1],
                MouseButton.Middle => currentMouseState.Buttons[2] && !s_mouseState.Buttons[2],
                _ => false
            },
            InputState.Pressed => button switch
            {
                MouseButton.Left => currentMouseState.Buttons[0],
                MouseButton.Right => currentMouseState.Buttons[1],
                MouseButton.Middle => currentMouseState.Buttons[2],
                _ => false
            },
            InputState.Up => button switch
            {
                MouseButton.Left => !currentMouseState.Buttons[0] && s_mouseState.Buttons[0],
                MouseButton.Right => !currentMouseState.Buttons[1] && s_mouseState.Buttons[1],
                MouseButton.Middle => !currentMouseState.Buttons[2] && s_mouseState.Buttons[2],
                _ => false
            },
            _ => false
        };
    }

    public static Vector2 GetAxis() =>
        s_axis.IsNaN() ? Vector2.Zero : s_axis;

    public static Vector2 GetJoystickAxis() =>
        s_joystickAxis.IsNaN() ? Vector2.Zero : s_axis;

    public static Vector2 GetMouseDelta() =>
        s_mouseDelta.IsNaN() ? Vector2.Zero : s_mouseDelta;

    public static Vector2 GetMousePosition() =>
        s_mousePosition.IsNaN() ? Vector2.Zero : s_mousePosition;

    public static Vector2 GetRawMousePosition() =>
        s_mousePosition;

    public static int GetMouseWheel() =>
        s_mouseWheel;

    public static void LockMouse(bool b) => s_lockMouse = b;
}
