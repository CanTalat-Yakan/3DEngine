using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using System;
using System.Numerics;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.System;

namespace Engine.Utilities
{
    public enum EMouseButton
    {
        ISLEFTBUTTONPRESSED,
        ISRIGHTBUTTONPRESSED,
        ISMIDDLEBUTTONSPRESSED
    }
    
    public enum EInputState 
    { 
        DOWN, 
        PRESSED, 
        UP 
    }
    
    public class Input
    {
        public static Input Instance { get; private set; }

        private Dictionary<VirtualKey, bool[]> _virtualKeyDic = new Dictionary<VirtualKey, bool[]>();
        private List<VirtualKey> _bufferKeys = new List<VirtualKey>();

        private Dictionary<EMouseButton, bool[]> _pointerPointDic = new Dictionary<EMouseButton, bool[]>();
        private List<EMouseButton> _bufferPoints = new List<EMouseButton>();

        private PointerPoint _pointer;
        private int _mouseWheelDelta;
        private Vector2 _axis = Vector2.Zero;
        private Vector2 _mouseAxis = Vector2.Zero;
        private Point _pointerPosition = new Point(), tmpPoint = new Point();

        public Input()
        {
            #region //Set Instance
            Instance = this;
            #endregion
        }

        public void Update()
        {
            if (_pointer != null)
            {
                _mouseAxis.X = -(float)(tmpPoint.X - _pointerPosition.X);
                _mouseAxis.Y = (float)(tmpPoint.Y - _pointerPosition.Y);

                tmpPoint = _pointerPosition;
            }

            _axis = new Vector2();
            if (GetKey(VirtualKey.W)) _axis.Y = 1;
            if (GetKey(VirtualKey.S)) _axis.Y = -1;
            if (GetKey(VirtualKey.A)) _axis.X = -1;
            if (GetKey(VirtualKey.D)) _axis.X = 1;
        }

        public void LateUpdate()
        {
            foreach (var item in _bufferKeys)
            {
                _virtualKeyDic[item][(int)EInputState.DOWN] = false;
                _virtualKeyDic[item][(int)EInputState.UP] = false;
            }
            foreach (var item in _bufferPoints)
            {
                _pointerPointDic[item][(int)EInputState.DOWN] = false;
                _pointerPointDic[item][(int)EInputState.UP] = false;
            }

            _bufferKeys.Clear();
            _bufferPoints.Clear();

            _mouseWheelDelta = 0;
        }

        private void SetKeyDic(VirtualKey key, bool[] newBool)
        {
            if (!_virtualKeyDic.ContainsKey(key))
                _virtualKeyDic.Add(key, newBool);
            else
                _virtualKeyDic[key] = newBool;

            _bufferKeys.Add(key);
        }

        private void SetPointerDic(EMouseButton input, bool[] newBool)
        {
            if (!_pointerPointDic.ContainsKey(input))
                _pointerPointDic.Add(input, newBool);
            else
                _pointerPointDic[input] = newBool;

            _bufferPoints.Add(input);
        }

        public bool GetKey(VirtualKey key, EInputState state = EInputState.PRESSED)
        {
            if (_virtualKeyDic.ContainsKey(key))
                return _virtualKeyDic[key][(int)state];

            return false;
        }

        public bool GetButton(EMouseButton button, EInputState state = EInputState.PRESSED)
        {
            if (_pointerPointDic.ContainsKey(button))
                return _pointerPointDic[button][(int)state];

            return false;
        }

        public Vector2 GetAxis() { return _axis; }

        public Vector2 GetMouseAxis() { return _mouseAxis; }

        public int GetMouseWheel() { return _mouseWheelDelta; }

        public void KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var newBool = new bool[3];
            newBool[(int)EInputState.DOWN] = true;
            newBool[(int)EInputState.PRESSED] = true;
            newBool[(int)EInputState.UP] = false;

            SetKeyDic(e.Key, newBool);

            e.Handled = true;
        }

        public void KeyUp(object sender, KeyRoutedEventArgs e) //CoreWindow sender, KeyEventArgs e)
        {
            var newBool = new bool[3];
            newBool[(int)EInputState.DOWN] = false;
            newBool[(int)EInputState.PRESSED] = false;
            newBool[(int)EInputState.UP] = true;

            SetKeyDic(e.Key, newBool);

            e.Handled = true;
        }

        public void PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                _pointer = e.GetCurrentPoint(null);

                var newBool = new bool[3];
                newBool[(int)EInputState.DOWN] = true;
                newBool[(int)EInputState.PRESSED] = true;
                newBool[(int)EInputState.UP] = false;

                if (_pointer.Properties.IsLeftButtonPressed)
                    SetPointerDic(EMouseButton.ISLEFTBUTTONPRESSED, newBool);

                if (_pointer.Properties.IsMiddleButtonPressed)
                    SetPointerDic(EMouseButton.ISMIDDLEBUTTONSPRESSED, newBool);

                if (_pointer.Properties.IsRightButtonPressed)
                    SetPointerDic(EMouseButton.ISRIGHTBUTTONPRESSED, newBool);
            }

            e.Handled = true;
        }

        public void PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                _pointer = e.GetCurrentPoint(null);

                var newBool = new bool[3];
                newBool[(int)EInputState.DOWN] = false;
                newBool[(int)EInputState.PRESSED] = false;
                newBool[(int)EInputState.UP] = true;

                if (!_pointer.Properties.IsLeftButtonPressed)
                    SetPointerDic(EMouseButton.ISLEFTBUTTONPRESSED, newBool);

                if (!_pointer.Properties.IsMiddleButtonPressed)
                    SetPointerDic(EMouseButton.ISMIDDLEBUTTONSPRESSED, newBool);

                if (!_pointer.Properties.IsRightButtonPressed)
                    SetPointerDic(EMouseButton.ISRIGHTBUTTONPRESSED, newBool);
            }
            e.Handled = true;
        }

        public void PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                _pointer = e.GetCurrentPoint(null);

                _mouseWheelDelta = Math.Clamp(_pointer.Properties.MouseWheelDelta, -1, 1);
            }

            e.Handled = true;
        }

        public void PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                _pointer = e.GetCurrentPoint(null);

                _pointerPosition = _pointer.Position;
            }

            e.Handled = true;
        }

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

    }
}
