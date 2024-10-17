global using Key = Vortice.DirectInput.Key;

using System.Linq;

using Vortice.DirectInput;

using Engine.Interoperation;

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
    private static bool s_lockMouse = false;

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

        if (s_lockMouse)
            LockMouse();
        else
            s_lockedmousePosition = s_mousePosition;
    }
}

public sealed partial class Input
{
    public static bool GetKey(Key key, InputState state = InputState.Pressed) =>
        state switch
        {
            InputState.Down => s_currentSnapshot.KeyboardState.IsPressed(key) && !s_currentSnapshot.PreviousKeyboardState.IsPressed(key),
            InputState.Pressed => s_currentSnapshot.KeyboardState.IsPressed(key),
            InputState.Up => !s_currentSnapshot.KeyboardState.IsPressed(key) && s_currentSnapshot.PreviousKeyboardState.IsPressed(key),
            _ => false
        };

    public static bool GetButton(MouseButton button, InputState state = InputState.Pressed) =>
        state switch
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

    public static Vector2 GetAxis() =>
        s_currentSnapshot.Axis.IsNaN() ? Vector2.Zero : s_currentSnapshot.Axis;

    public static Vector2 GetJoystickAxis() =>
        s_currentSnapshot.JoystickAxis.IsNaN() ? Vector2.Zero : s_currentSnapshot.JoystickAxis;

    public static Vector2 GetMouseDelta() =>
        s_currentSnapshot.MouseDelta.IsNaN() ? Vector2.Zero : s_currentSnapshot.MouseDelta;

    public static Vector2 GetMousePosition() =>
        s_currentSnapshot.MousePosition.IsNaN() ? Vector2.Zero : s_currentSnapshot.MousePosition;

    public static Vector2 GetRawMousePosition() =>
        s_currentSnapshot.MousePosition;

    public static int GetMouseWheel() =>
        s_currentSnapshot.MouseWheel;
}

public sealed partial class Input
{
    public static void SetLockMouse(bool lockMouse) =>
        s_lockMouse = lockMouse;

    private static void LockMouse()
    {
        User32.SetCursorPos((int)s_lockedmousePosition.X, (int)s_lockedmousePosition.Y);
        User32.SetCursor(User32.LoadCursor(IntPtr.Zero, null));

        if (GetKey(Key.Escape, InputState.Pressed))
            SetLockMouse(false);
    }
}
