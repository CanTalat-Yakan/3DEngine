using Vortice.DirectInput;

namespace Engine.Utilities
{
    internal class DInput
    {
        private static IDirectInput8 directInput;
        private static IDirectInputDevice8 keyboard;
        private static IDirectInputDevice8 mouse;
        private static MouseState mouseState;
        private static KeyboardState keyboardState;

        public static void Initialize(IntPtr windowHandle)
        {
            directInput = new(windowHandle);
            keyboard = new(directInput.NativePointer);
            keyboard.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);

            mouse = new(directInput.NativePointer);
            mouse.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
        }

        public static void Update()
        {
            keyboard.Acquire();
            keyboard.Poll();
            mouse.Acquire();
            mouse.Poll();
        }

        public static void LateUpdate()
        {
            // Perform any late update tasks here
        }

        public static bool GetKey(Key key, InputState state = InputState.Pressed)
        {
            var keyState = keyboard.GetCurrentKeyboardState();
            return state switch
            {
                InputState.Down => keyState.IsPressed(key),
                InputState.Pressed => keyState.IsPressed(key) && !keyboardState.IsPressed(key),
                InputState.Up => !keyState.IsPressed(key) && keyboardState.IsPressed(key),
                _ => false
            };
        }

        public static bool GetButton(MouseButton button, InputState state = InputState.Pressed)
        {
            var mouseState = mouse.GetCurrentMouseState();
            return true;
            //return state switch
            //{
            //    InputState.Down => button switch
            //    {
            //        MouseButton.IsLeftButtonPressed => mouseState.IsPressed(MouseKeyFlags.Left),
            //        MouseButton.IsRightButtonPressed => mouseState.IsPressed(MouseKeyFlags.Right),
            //        MouseButton.IsMiddleButtonPressed => mouseState.IsPressed(MouseKeyFlags.Middle),
            //        _ => false
            //    },
            //    InputState.Pressed => button switch
            //    {
            //        MouseButton.IsLeftButtonPressed => mouseState.IsPressed(MouseKeyFlags.Left) && !mouseStateState[MouseButton.IsLeftButtonPressed],
            //        MouseButton.IsRightButtonPressed => mouseState.IsPressed(MouseKeyFlags.Right) && !mouseStateState[MouseButton.IsRightButtonPressed],
            //        MouseButton.IsMiddleButtonPressed => mouseState.IsPressed(MouseKeyFlags.Middle) && !mouseStateState[MouseButton.IsMiddleButtonPressed],
            //        _ => false
            //    },
            //    InputState.Up => button switch
            //    {
            //        MouseButton.IsLeftButtonPressed => !mouseState.IsPressed(MouseKeyFlags.Left) && mouseStateState[MouseButton.IsLeftButtonPressed],
            //        MouseButton.IsRightButtonPressed => !mouseState.IsPressed(MouseKeyFlags.Right) && mouseStateState[MouseButton.IsRightButtonPressed],
            //        MouseButton.IsMiddleButtonPressed => !mouseState.IsPressed(MouseKeyFlags.Middle) && mouseStateState[MouseButton.IsMiddleButtonPressed],
            //        _ => false
            //    },
            //    _ => false
            //};
        }

        //public static Vector2 GetAxis()
        //{
        //    // Implement the method to get input axis
        //}

        //public static Vector2 GetMouseAxis()
        //{
        //    // Implement the method to get mouse axis
        //}

        //public static Vector2 GetMousePosition()
        //{
        //    // Implement the method to get mouse position
        //}

        //public static int GetMouseWheel()
        //{
        //    // Implement the method to get mouse wheel input
        //}
    }
}
