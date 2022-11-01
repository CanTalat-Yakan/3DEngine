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

        Dictionary<VirtualKey, bool[]> m_virtualKeyDic = new Dictionary<VirtualKey, bool[]>();
        List<VirtualKey> m_bufferKeys = new List<VirtualKey>();


        Dictionary<EMouseButton, bool[]> m_pointerPointDic = new Dictionary<EMouseButton, bool[]>();
        List<EMouseButton> m_bufferPoints = new List<EMouseButton>();

        PointerPoint m_pointer;
        int m_mouseWheelDelta;
        Vector2 m_axis = Vector2.Zero;
        Vector2 m_mouseAxis = Vector2.Zero;
        Point m_pointerPosition = new Point(), m_tmpPoint = new Point();


        public Input()
        {
            #region //Set Instance
            Instance = this;
            #endregion
        }

        public void Update()
        {
            if (m_pointer != null)
            {
                //if (m_mouseAxis != Vector2.Zero)
                    //COutput.Log("X: " + m_mouseAxis.X.ToString() + ", " + "Y: " + m_mouseAxis.Y.ToString());

                m_mouseAxis.X = -(float)(m_tmpPoint.X - m_pointerPosition.X);
                m_mouseAxis.Y = (float)(m_tmpPoint.Y - m_pointerPosition.Y);

                m_tmpPoint = m_pointerPosition;
            }

            m_axis = new Vector2();
            if (GetKey(VirtualKey.W)) m_axis.Y = 1;
            if (GetKey(VirtualKey.S)) m_axis.Y = -1;
            if (GetKey(VirtualKey.A)) m_axis.X = -1;
            if (GetKey(VirtualKey.D)) m_axis.X = 1;
        }

        public void LateUpdate()
        {
            foreach (var item in m_bufferKeys)
            {
                m_virtualKeyDic[item][(int)EInputState.DOWN] = false;
                m_virtualKeyDic[item][(int)EInputState.UP] = false;
            }
            foreach (var item in m_bufferPoints)
            {
                m_pointerPointDic[item][(int)EInputState.DOWN] = false;
                m_pointerPointDic[item][(int)EInputState.UP] = false;
            }

            m_bufferKeys.Clear();
            m_bufferPoints.Clear();

            m_mouseWheelDelta = 0;
        }


        public bool GetKey(VirtualKey _key, EInputState _state = EInputState.PRESSED)
        {
            if (m_virtualKeyDic.ContainsKey(_key))
                return m_virtualKeyDic[_key][(int)_state];

            return false;
        }
        public bool GetButton(EMouseButton _button, EInputState _state = EInputState.PRESSED)
        {
            if (m_pointerPointDic.ContainsKey(_button))
                return m_pointerPointDic[_button][(int)_state];

            return false;
        }
        public Vector2 GetAxis() { return m_axis; }
        public Vector2 GetMouseAxis() { return m_mouseAxis; }
        public int GetMouseWheel() { return m_mouseWheelDelta; }

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
            if (!m_virtualKeyDic.ContainsKey(_key))
                m_virtualKeyDic.Add(_key, _newBool);
            else
                m_virtualKeyDic[_key] = _newBool;

            m_bufferKeys.Add(_key);
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
            if (!m_pointerPointDic.ContainsKey(_input))
                m_pointerPointDic.Add(_input, _newBool);
            else
                m_pointerPointDic[_input] = _newBool;

            m_bufferPoints.Add(_input);
        }
        internal void PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                m_pointer = e.GetCurrentPoint(null);

                var newBool = new bool[3];
                newBool[(int)EInputState.DOWN] = true;
                newBool[(int)EInputState.PRESSED] = true;
                newBool[(int)EInputState.UP] = false;

                if (m_pointer.Properties.IsLeftButtonPressed)
                    SetPointerDic(EMouseButton.IsLeftButtonPressed, newBool);

                if (m_pointer.Properties.IsMiddleButtonPressed)
                    SetPointerDic(EMouseButton.IsMiddleButtonPressed, newBool);

                if (m_pointer.Properties.IsRightButtonPressed)
                    SetPointerDic(EMouseButton.IsRightButtonPressed, newBool);
            }

            e.Handled = true;
        }
        internal void PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                m_pointer = e.GetCurrentPoint(null);

                var newBool = new bool[3];
                newBool[(int)EInputState.DOWN] = false;
                newBool[(int)EInputState.PRESSED] = false;
                newBool[(int)EInputState.UP] = true;

                if (!m_pointer.Properties.IsLeftButtonPressed)
                    SetPointerDic(EMouseButton.IsLeftButtonPressed, newBool);

                if (!m_pointer.Properties.IsMiddleButtonPressed)
                    SetPointerDic(EMouseButton.IsMiddleButtonPressed, newBool);

                if (!m_pointer.Properties.IsRightButtonPressed)
                    SetPointerDic(EMouseButton.IsRightButtonPressed, newBool);
            }
            e.Handled = true;
        }
        internal void PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                m_pointer = e.GetCurrentPoint(null);

                m_mouseWheelDelta = Math.Clamp(m_pointer.Properties.MouseWheelDelta, -1, 1);
            }

            e.Handled = true;
        }

        internal void PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                m_pointer = e.GetCurrentPoint(null);

                m_pointerPosition = m_pointer.Position;
            }

            e.Handled = true;
        }
    }
}
