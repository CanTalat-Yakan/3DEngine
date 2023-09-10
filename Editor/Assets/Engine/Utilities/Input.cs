using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.System;

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

    public sealed class Input
    {
        private static Dictionary<VirtualKey, bool[]> _virtualKeyDic = new();
        private static List<VirtualKey> _bufferKeys = new();

        private static Dictionary<MouseButton, bool[]> _pointerPointDic = new();
        private static List<MouseButton> _bufferPoints = new();

        private static PointerPoint _pointer;
        private static int _mouseWheelDelta;

        private static Vector2 _axis = Vector2.Zero;
        private static Vector2 _mouseAxis = Vector2.Zero;

        private static Point _pointerPosition = new(), tmpPoint = new();

        public static void Update()
        {
            // Check if pointer is not null.
            if (_pointer is not null)
            {
                // Calculate mouse axis based on the difference between the current and previous pointer positions.
                _mouseAxis.X = -(float)(tmpPoint.X - _pointerPosition.X);
                _mouseAxis.Y = (float)(tmpPoint.Y - _pointerPosition.Y);
                // Update the previous pointer position.
                tmpPoint = _pointerPosition;
            }

            // Reset axis vector.
            _axis = Vector2.Zero;
            // Update axis based on keyboard input.
            if (GetKey(VirtualKey.W)) _axis.Y++;
            if (GetKey(VirtualKey.S)) _axis.Y--;
            if (GetKey(VirtualKey.A)) _axis.X--;
            if (GetKey(VirtualKey.D)) _axis.X++;
        }

        public static void LateUpdate()
        {
            // Reset the input state of all keys and pointer points.
            foreach (var item in _bufferKeys)
            {
                _virtualKeyDic[item][(int)InputState.Down] = false;
                _virtualKeyDic[item][(int)InputState.Up] = false;
            }
            // Reset the input state of all pointer points.
            foreach (var item in _bufferPoints)
            {
                _pointerPointDic[item][(int)InputState.Down] = false;
                _pointerPointDic[item][(int)InputState.Up] = false;
            }

            // Clear the buffer of keys and pointer points.
            _bufferKeys.Clear();
            _bufferPoints.Clear();

            // Reset the mouse wheel delta.
            _mouseWheelDelta = 0;
        }

        public static bool GetKey(VirtualKey key, InputState state = InputState.Pressed)
        {
            // Check if the dictionary contains the key.
            if (_virtualKeyDic.ContainsKey(key))
                // Return the value of the specified state for the key.
                return _virtualKeyDic[key][(int)state];

            // Return false if the key is not found in the dictionary.
            return false;
        }

        public static bool GetButton(MouseButton button, InputState state = InputState.Pressed)
        {
            // Check if the input button is stored in the dictionary.
            if (_pointerPointDic.ContainsKey(button))
                // Return the state of the input button (Down/Up) stored in the dictionary.
                return _pointerPointDic[button][(int)state];

            // If the input button is not found in the dictionary, return false.
            return false;
        }

        public static Vector2 GetAxis() =>
            _axis.IsNaN() ? Vector2.Zero : _axis;

        public static Vector2 GetMouseAxis() =>
            _mouseAxis.IsNaN() ? Vector2.Zero : _mouseAxis;

        public static Point GetMousePosition() =>
            _pointerPosition;

        public static int GetMouseWheel() => 
            _mouseWheelDelta;

        public static void KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Create an array of booleans to store the state of the virtual key.
            var newBool = new bool[3];

            // Set the "Down" and "Pressed" states to true for the virtual key.
            newBool[(int)InputState.Down] = true;
            newBool[(int)InputState.Pressed] = true;
            // Set the "Up" state to false for the virtual key.
            newBool[(int)InputState.Up] = false;

            // Update the virtual key state in the virtual key dictionary.
            SetKeyDic(e.Key, newBool);

            // Mark the event as handled to prevent it from being processed further.
            e.Handled = true;
        }

        public static void KeyUp(object sender, KeyRoutedEventArgs e)
        {
            // Create an array of booleans to store the state of the virtual key.
            var newBool = new bool[3];

            // Set the "Down" and "Pressed" states to false for the virtual key.
            newBool[(int)InputState.Down] = false;
            newBool[(int)InputState.Pressed] = false;
            // Set the "Up" state to true for the virtual key.
            newBool[(int)InputState.Up] = true;

            // Update the virtual key state in the virtual key dictionary.
            SetKeyDic(e.Key, newBool);

            // Mark the event as handled to prevent it from being processed further.
            e.Handled = true;
        }

        public static void PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Check if the pointer is a mouse.
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                // Store the current point of the mouse.
                _pointer = e.GetCurrentPoint(null);

                // Create an array of booleans to store the state of the virtual mouse button.
                var newBool = new bool[3];

                // Set the "Down" and "Pressed" states to true for the virtual mouse button.
                newBool[(int)InputState.Down] = true;
                newBool[(int)InputState.Pressed] = true;
                // Set the "Up" state to false for the virtual mouse button.
                newBool[(int)InputState.Up] = false;

                // Check if the left mouse button is pressed.
                if (_pointer.Properties.IsLeftButtonPressed)
                    // Update the dictionary for the left mouse button with the new state.
                    SetPointerDic(MouseButton.IsLeftButtonPressed, newBool);

                // Check if the middle mouse button is pressed.
                if (_pointer.Properties.IsMiddleButtonPressed)
                    // Update the dictionary for the middle mouse button with the new state.
                    SetPointerDic(MouseButton.IsMiddleButtonPressed, newBool);

                // Check if the right mouse button is pressed.
                if (_pointer.Properties.IsRightButtonPressed)
                    // Update the dictionary for the right mouse button with the new state.
                    SetPointerDic(MouseButton.IsRightButtonPressed, newBool);
            }

            // Mark the event as handled to prevent it from being processed further.
            e.Handled = true;
        }

        public static void PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                // Store the current point of the mouse.
                _pointer = e.GetCurrentPoint(null);

                // Create an array of booleans to store the state of the virtual mouse button.
                var newBool = new bool[3];

                // Set the "Down" and "Pressed" states to false for the virtual mouse button.
                newBool[(int)InputState.Down] = false;
                newBool[(int)InputState.Pressed] = false;
                // Set the "Up" state to true for the virtual mouse button.
                newBool[(int)InputState.Up] = true;

                // Check if the left mouse button is not pressed.
                if (!_pointer.Properties.IsLeftButtonPressed)
                    // Update the dictionary for the left mouse button with the new state.
                    SetPointerDic(MouseButton.IsLeftButtonPressed, newBool);

                // Check if the middle mouse button is not pressed.
                if (!_pointer.Properties.IsMiddleButtonPressed)
                    // Update the dictionary for the middle mouse button with the new state.
                    SetPointerDic(MouseButton.IsMiddleButtonPressed, newBool);

                // Check if the right mouse button is not pressed.
                if (!_pointer.Properties.IsRightButtonPressed)
                    // Update the dictionary for the right mouse button with the new state.
                    SetPointerDic(MouseButton.IsRightButtonPressed, newBool);
            }

            // Mark the event as handled to prevent it from being processed further.
            e.Handled = true;
        }

        public static void PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            // Check if the type of the pointer device that generated the event is a mouse.
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                // Store the current point of the mouse.
                _pointer = e.GetCurrentPoint(null);

                // Get the mouse wheel delta from the event properties
                // and store the clamped value between -1 and 1.
                _mouseWheelDelta = Math.Clamp(_pointer.Properties.MouseWheelDelta, -1, 1);
            }

            // Mark the event as handled to prevent it from being processed further.
            e.Handled = true;
        }

        public static void PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // Check if the type of the pointer device that generated the event is a mouse.
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                // Store the current point of the mouse.
                _pointer = e.GetCurrentPoint(null);

                // Get the mouse position from the event.
                _pointerPosition = _pointer.Position;
            }

            // Mark the event as handled to prevent it from being processed further.
            e.Handled = true;
        }

        private static void SetKeyDic(VirtualKey key, bool[] newBool)
        {
            // Check if the virtual key is already in the virtual key dictionary.
            if (!_virtualKeyDic.ContainsKey(key))
                // If not, add the virtual key to the dictionary with the new boolean value.
                _virtualKeyDic.Add(key, newBool);
            else
                // If the virtual key is already in the dictionary, update its value to the new boolean value.
                _virtualKeyDic[key] = newBool;

            // Add the virtual key to the buffer keys list.
            _bufferKeys.Add(key);
        }

        private static void SetPointerDic(MouseButton input, bool[] newBool)
        {
            // Check if the current pointer point is already in the dictionary.
            if (!_pointerPointDic.ContainsKey(input))
                // If not, add the current pointer point to the dictionary.
                _pointerPointDic.Add(input, newBool);
            else
                // If yes, update the current pointer point in the dictionary.
                _pointerPointDic[input] = newBool;

            // Add the current pointer point to the buffer list.
            _bufferPoints.Add(input);
        }
    }
}
