using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using System;
using System.Numerics;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Editor.Controls;

namespace Engine.Utilities
{
    public enum EMouseButton
    {
        IsLeftButtonPressed,
        IsRightButtonPressed,
        IsMiddleButtonPressed
    }
    internal class Input
    {
        public static Input Instance { get; private set; }

        public enum EInputState { DOWN, PRESSED, UP }

        Dictionary<VirtualKey, bool[]> virtualKeyDic = new Dictionary<VirtualKey, bool[]>();
        List<VirtualKey> bufferKeys = new List<VirtualKey>();


        Dictionary<EMouseButton, bool[]> pointerPointDic = new Dictionary<EMouseButton, bool[]>();
        List<EMouseButton> bufferPoints = new List<EMouseButton>();

        PointerPoint pointer;
        int mouseWheelDelta;
        Vector2 axis = Vector2.Zero;
        Vector2 mouseAxis = Vector2.Zero;
        Point pointerPosition = new Point(), tmpPoint = new Point();


        public Input()
        {
            #region //Set Instance
            Instance = this;
            #endregion
        }

        public void Update()
        {
            if (pointer != null)
            {
                //if (m_mouseAxis != Vector2.Zero)
                    //COutput.Log("X: " + m_mouseAxis.X.ToString() + ", " + "Y: " + m_mouseAxis.Y.ToString());

                mouseAxis.X = -(float)(tmpPoint.X - pointerPosition.X);
                mouseAxis.Y = (float)(tmpPoint.Y - pointerPosition.Y);

                tmpPoint = pointerPosition;
            }

            axis = new Vector2();
            if (GetKey(VirtualKey.W)) axis.Y = 1;
            if (GetKey(VirtualKey.S)) axis.Y = -1;
            if (GetKey(VirtualKey.A)) axis.X = -1;
            if (GetKey(VirtualKey.D)) axis.X = 1;
        }

        public void LateUpdate()
        {
            foreach (var item in bufferKeys)
            {
                virtualKeyDic[item][(int)EInputState.DOWN] = false;
                virtualKeyDic[item][(int)EInputState.UP] = false;
            }
            foreach (var item in bufferPoints)
            {
                pointerPointDic[item][(int)EInputState.DOWN] = false;
                pointerPointDic[item][(int)EInputState.UP] = false;
            }

            bufferKeys.Clear();
            bufferPoints.Clear();

            mouseWheelDelta = 0;
        }


        public bool GetKey(VirtualKey _key, EInputState _state = EInputState.PRESSED)
        {
            if (virtualKeyDic.ContainsKey(_key))
                return virtualKeyDic[_key][(int)_state];

            return false;
        }
        public bool GetButton(EMouseButton _button, EInputState _state = EInputState.PRESSED)
        {
            if (pointerPointDic.ContainsKey(_button))
                return pointerPointDic[_button][(int)_state];

            return false;
        }
        public Vector2 GetAxis() { return axis; }
        public Vector2 GetMouseAxis() { return mouseAxis; }
        public int GetMouseWheel() { return mouseWheelDelta; }

        public bool SetPointerInBounds()
        {
            //if (m_pointer.Position.X <= 0)
            //{
            //    m_tmpPoint = new Point(Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds.Width, m_pointer.Position.Y + 42);
            //    CoreWindow.GetForCurrentThread().PointerPosition = m_tmpPoint;
            //    m_pointerPosition = m_tmpPoint;
            //    m_mouseAxis = Vector2.Zero;
            //    return false;
            //}
            //else if (m_pointer.Position.X >= Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds.Width - 1)
            //{
            //    m_tmpPoint = new Point(0, m_pointer.Position.Y + 42);
            //    CoreWindow.GetForCurrentThread().PointerPosition = m_tmpPoint;
            //    m_pointerPosition = m_tmpPoint;
            //    m_mouseAxis = Vector2.Zero;
            //    return false;
            //}
            //else if (m_pointer.Position.Y <= 0)
            //{
            //    m_tmpPoint = new Point(m_pointer.Position.X, Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds.Height + 42);
            //    CoreWindow.GetForCurrentThread().PointerPosition = m_tmpPoint;
            //    m_pointerPosition = m_tmpPoint;
            //    m_mouseAxis = Vector2.Zero;
            //    return false;
            //}
            //else if (m_pointer.Position.Y >= Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds.Height - 1)
            //{
            //    m_tmpPoint = new Point(m_pointer.Position.X, 0);
            //    CoreWindow.GetForCurrentThread().PointerPosition = m_tmpPoint;
            //    m_pointerPosition = m_tmpPoint;
            //    m_mouseAxis = Vector2.Zero;
            //    return false;
            //}

            return true;
        }

        void SetKeyDic(VirtualKey _key, bool[] _newBool)
        {
            if (!virtualKeyDic.ContainsKey(_key))
                virtualKeyDic.Add(_key, _newBool);
            else
                virtualKeyDic[_key] = _newBool;

            bufferKeys.Add(_key);
        }
        internal void KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var newBool = new bool[3];
            newBool[(int)EInputState.DOWN] = true;
            newBool[(int)EInputState.PRESSED] = true;
            newBool[(int)EInputState.UP] = false;

            SetKeyDic(e.Key, newBool);

            e.Handled = true;
        }
        internal void KeyUp(object sender, KeyRoutedEventArgs e) //CoreWindow sender, KeyEventArgs e)
        {
            var newBool = new bool[3];
            newBool[(int)EInputState.DOWN] = false;
            newBool[(int)EInputState.PRESSED] = false;
            newBool[(int)EInputState.UP] = true;

            SetKeyDic(e.Key, newBool);

            e.Handled = true;
        }


        void SetPointerDic(EMouseButton _input, bool[] _newBool)
        {
            if (!pointerPointDic.ContainsKey(_input))
                pointerPointDic.Add(_input, _newBool);
            else
                pointerPointDic[_input] = _newBool;

            bufferPoints.Add(_input);
        }
        internal void PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                pointer = e.GetCurrentPoint(null);

                var newBool = new bool[3];
                newBool[(int)EInputState.DOWN] = true;
                newBool[(int)EInputState.PRESSED] = true;
                newBool[(int)EInputState.UP] = false;

                if (pointer.Properties.IsLeftButtonPressed)
                    SetPointerDic(EMouseButton.IsLeftButtonPressed, newBool);

                if (pointer.Properties.IsMiddleButtonPressed)
                    SetPointerDic(EMouseButton.IsMiddleButtonPressed, newBool);

                if (pointer.Properties.IsRightButtonPressed)
                    SetPointerDic(EMouseButton.IsRightButtonPressed, newBool);
            }

            e.Handled = true;
        }
        internal void PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                pointer = e.GetCurrentPoint(null);

                var newBool = new bool[3];
                newBool[(int)EInputState.DOWN] = false;
                newBool[(int)EInputState.PRESSED] = false;
                newBool[(int)EInputState.UP] = true;

                if (!pointer.Properties.IsLeftButtonPressed)
                    SetPointerDic(EMouseButton.IsLeftButtonPressed, newBool);

                if (!pointer.Properties.IsMiddleButtonPressed)
                    SetPointerDic(EMouseButton.IsMiddleButtonPressed, newBool);

                if (!pointer.Properties.IsRightButtonPressed)
                    SetPointerDic(EMouseButton.IsRightButtonPressed, newBool);
            }
            e.Handled = true;
        }
        internal void PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                pointer = e.GetCurrentPoint(null);

                mouseWheelDelta = Math.Clamp(pointer.Properties.MouseWheelDelta, -1, 1);
            }

            e.Handled = true;
        }

        internal void PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                pointer = e.GetCurrentPoint(null);

                pointerPosition = pointer.Position;
            }

            e.Handled = true;
        }
    }
}
