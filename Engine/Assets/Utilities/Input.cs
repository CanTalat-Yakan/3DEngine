using Microsoft.VisualBasic.Devices;
using System.Linq;
using Vortice.DirectInput;

namespace Engine.Utilities
{
    public enum MouseButton
    {
        IsLeftButtonPressed,
        IsRightButtonPressed,
        IsMiddleButtonPressed
    }

    public enum InputState
    {
        Down,
        Pressed,
        Up
    }

    internal class Input
    {
        private static IDirectInput8 _directInput;
        private static IDirectInputDevice8 _keyboard;
        private static IDirectInputDevice8 _mouse;

        private static MouseState _mouseState;
        private static KeyboardState _keyboardState;

        private static Vector2 _axis = Vector2.Zero;
        private static Vector2 _mouseAxis = Vector2.Zero;

        public static void Initialize(IntPtr windowHandle)
        {
            _directInput = DInput.DirectInput8Create();

            var mouseGuid = _directInput.GetDevices(DeviceClass.Pointer, DeviceEnumerationFlags.AttachedOnly).First().InstanceGuid;
            _mouse = _directInput.CreateDevice(mouseGuid);
            _mouse.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            _mouse.SetDataFormat<RawKeyboardState>();
            _mouse.Acquire();
            _mouse.Poll();
            _mouse.GetCurrentMouseState(ref _mouseState);

            var keyboardGuid = _directInput.GetDevices(DeviceClass.Keyboard, DeviceEnumerationFlags.AttachedOnly).First().InstanceGuid;
            _keyboard = _directInput.CreateDevice(keyboardGuid);
            _keyboard.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            _keyboard.SetDataFormat<RawKeyboardState>();
            _keyboard.Acquire();
            _keyboard.Poll();
            _keyboardState = new();
            _keyboard.GetCurrentKeyboardState(ref _keyboardState);
        }

        public static void Dispose()
        {
            _directInput.Dispose();
            _keyboard.Dispose();
            _mouse.Dispose();
        }

        public static void Update()
        {
            _keyboard.Acquire();
            _keyboard.Poll();

            _mouse.Acquire();
            _mouse.Poll();

            // Calculate mouse axis based on the difference between the current and previous pointer positions.
            _mouseAxis.X = -_mouseState.X - _mouse.GetCurrentMouseState().X;
            _mouseAxis.Y = _mouseState.Y - _mouse.GetCurrentMouseState().Y;

            // Reset axis vector.
            _axis = Vector2.Zero;
            // Update axis based on keyboard input.
            if (GetKey(Key.W)) _axis.Y++;
            if (GetKey(Key.S)) _axis.Y--;
            if (GetKey(Key.D)) _axis.X++;
            if (GetKey(Key.A)) _axis.X--;
        }

        public static void LateUpdate()
        {
            _mouseState = _mouse.GetCurrentMouseState();
            _keyboardState = _keyboard.GetCurrentKeyboardState();
        }

        public static bool GetKey(Key key, InputState state = InputState.Pressed)
        {
            var currentKeyboardState = _keyboard.GetCurrentKeyboardState();
            return state switch
            {
                InputState.Down => currentKeyboardState.IsPressed(key) && !_keyboardState.IsPressed(key),
                InputState.Pressed => currentKeyboardState.IsPressed(key),
                InputState.Up => !currentKeyboardState.IsPressed(key) && _keyboardState.IsPressed(key),
                _ => false
            };
        }

        public static bool GetButton(MouseButton button, InputState state = InputState.Pressed)
        {
            var currentMouseState = _mouse.GetCurrentMouseState();

            return state switch
            {
                InputState.Down => button switch
                {
                    MouseButton.IsLeftButtonPressed => currentMouseState.Buttons[0] && !_mouseState.Buttons[0],
                    MouseButton.IsRightButtonPressed => currentMouseState.Buttons[1] && !_mouseState.Buttons[1],
                    MouseButton.IsMiddleButtonPressed => currentMouseState.Buttons[2] && !_mouseState.Buttons[2],
                    _ => false
                },
                InputState.Pressed => button switch
                {
                    MouseButton.IsLeftButtonPressed => currentMouseState.Buttons[0],
                    MouseButton.IsRightButtonPressed => currentMouseState.Buttons[1],
                    MouseButton.IsMiddleButtonPressed => currentMouseState.Buttons[2],
                    _ => false
                },
                InputState.Up => button switch
                {
                    MouseButton.IsLeftButtonPressed => !currentMouseState.Buttons[0] && _mouseState.Buttons[0],
                    MouseButton.IsRightButtonPressed => !currentMouseState.Buttons[1] && _mouseState.Buttons[1],
                    MouseButton.IsMiddleButtonPressed => !currentMouseState.Buttons[2] && _mouseState.Buttons[2],
                    _ => false
                },
                _ => false
            };
        }

        public static Vector2 GetAxis() =>
            _axis.IsNaN() ? Vector2.Zero : _axis;

        public static Vector2 GetMouseAxis() =>
            _mouseAxis.IsNaN() ? Vector2.Zero : _mouseAxis;

        public static Vector2 GetMousePosition() =>
            new Vector2(_mouse.GetCurrentMouseState().X, _mouse.GetCurrentMouseState().Y);

        public static int GetMouseWheel() =>
            _mouse.GetCurrentMouseState().Z;
    }
}
