﻿using System.Diagnostics;
using System.Linq;
using Vortice.DirectInput;
using Windows.Devices.Input;

namespace Engine.Utilities
{
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

    internal class Input
    {
        private static IDirectInput8 _directInput;
        private static IDirectInputDevice8 _keyboard;
        private static IDirectInputDevice8 _mouse;

        private static MouseState _mouseState = new();
        private static KeyboardState _keyboardState = new();

        private static Vector2 _axis = Vector2.Zero;
        private static Vector2 _mouseAxis = Vector2.Zero;
        private static Vector2 _mousePosition = Vector2.Zero;
        private static int _mouseDelta = 0;

        public static void Initialize(IntPtr windowHandle)
        {
            _directInput = DInput.DirectInput8Create();

            var _mouseGuid = _directInput.GetDevices(DeviceClass.Pointer, DeviceEnumerationFlags.AttachedOnly).First().InstanceGuid;
            _mouse = _directInput.CreateDevice(_mouseGuid);
            _mouse.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            _mouse.SetDataFormat<RawMouseState>();

            var _keyboardGuid = _directInput.GetDevices(DeviceClass.Keyboard, DeviceEnumerationFlags.AttachedOnly).First().InstanceGuid;
            _keyboard = _directInput.CreateDevice(_keyboardGuid);
            _keyboard.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            _keyboard.SetDataFormat<RawKeyboardState>();
        }

        public static void Dispose()
        {
            _directInput.Dispose();
            _keyboard.Dispose();
            _mouse.Dispose();
        }

        public static void Fetch()
        {
            _keyboard.Acquire();
            _keyboard.Poll();

            _mouse.Acquire();
            _mouse.Poll();
        }

        public static void Update()
        {
            try
            {
                // Calculate mouse axis based on the difference between the current and previous pointer positions.
                var currentMouseState = _mouse.GetCurrentMouseState();
                _mouseAxis.X = currentMouseState.X - _mouseState.X;
                _mouseAxis.Y = currentMouseState.Y - _mouseState.Y;
                _mouseAxis.Y *= -1; // The DirectX Y Coord starts at the top.

                _mousePosition.X = currentMouseState.X; 
                _mousePosition.Y = currentMouseState.Y;

                _mouseDelta = currentMouseState.Z;
            }
            catch (Exception) { }

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
            try
            {
                _mouseState = _mouse.GetCurrentMouseState();
                _keyboardState = _keyboard.GetCurrentKeyboardState();
            }
            catch (Exception) { }

        }

        public static bool GetKey(Key key, InputState state = InputState.Pressed)
        {
            KeyboardState currentKeyboardState = null;
            try
            {
                currentKeyboardState = _keyboard.GetCurrentKeyboardState();
            }
            catch (Exception) { return false; }

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
            MouseState currentMouseState = null;
            try
            {
                currentMouseState = _mouse.GetCurrentMouseState();
            }
            catch (Exception) { return false; }

            return state switch
            {
                InputState.Down => button switch
                {
                    MouseButton.Left => currentMouseState.Buttons[0] && !_mouseState.Buttons[0],
                    MouseButton.Right => currentMouseState.Buttons[1] && !_mouseState.Buttons[1],
                    MouseButton.Middle => currentMouseState.Buttons[2] && !_mouseState.Buttons[2],
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
                    MouseButton.Left => !currentMouseState.Buttons[0] && _mouseState.Buttons[0],
                    MouseButton.Right => !currentMouseState.Buttons[1] && _mouseState.Buttons[1],
                    MouseButton.Middle => !currentMouseState.Buttons[2] && _mouseState.Buttons[2],
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
            _mousePosition.IsNaN() ? Vector2.Zero : _mousePosition;

        public static int GetMouseDelta() =>
            _mouseDelta;
    }
}
