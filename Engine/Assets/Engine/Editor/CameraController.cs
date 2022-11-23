using System;
using System.Numerics;
using Windows.System;
using Engine.Components;
using Engine.Utilities;

namespace Engine.Editor
{
    internal class CameraController
    {
        public static float s_MovementSpeed = 2;

        public string Profile { get => _camera.Transform.ToString(); }

        private CameraComponent _camera;
        private Input _input;

        private float _rotationSpeed = 5;
        private Vector3 _direction;
        private Vector3 _rotation;

        public CameraController(CameraComponent camera)
        {
            _camera = camera;
            _input = Input.Instance;
        }

        public void Update()
        {
            MovementSpeedCalc();

            if (_input.GetButton(EMouseButton.ISMIDDLEBUTTONSPRESSED))
                if (_input.SetPointerInBounds())
                    ScreenMovement();

            if (_input.GetButton(EMouseButton.ISRIGHTBUTTONPRESSED))
                if (_input.SetPointerInBounds())
                {
                    TransformMovement();
                    CameraMovement();
                    HeightTransformMovement();
                }

            ScrollMovement();

            _camera.Transform.Position += _direction * (float)Time.s_Delta * s_MovementSpeed;
            _camera.Transform.EulerAngles -= _rotation * (float)Time.s_Delta * _rotationSpeed;

            _camera.Transform.EulerAngles.X = Math.Clamp(_camera.Transform.EulerAngles.X, -89, 89);

            _rotation = new Vector3();
            _direction = new Vector3();
        }

        private void MovementSpeedCalc()
        {
            if (_input.GetButton(EMouseButton.ISRIGHTBUTTONPRESSED)
                || _input.GetButton(EMouseButton.ISRIGHTBUTTONPRESSED))
                s_MovementSpeed += _input.GetMouseWheel();

            s_MovementSpeed = Math.Clamp(s_MovementSpeed, 0.1f, 10);
        }

        private void CameraMovement(int _horizontalFactor = 1, int _verticalFactor = 1) =>
            _rotation = new Vector3(_input.GetMouseAxis().Y, _input.GetMouseAxis().X, 0);

        private void TransformMovement() =>
            _direction = _camera.Transform.Forward * _input.GetAxis().Y + _camera.Transform.Right * _input.GetAxis().X;

        private void ScreenMovement() =>
            _direction -= _camera.Transform.Right * _input.GetMouseAxis().X * (float)Time.s_Delta + _camera.Transform.LocalUp * _input.GetMouseAxis().Y * (float)Time.s_Delta;

        private void ScrollMovement()
        {
            if (!_input.GetButton(EMouseButton.ISRIGHTBUTTONPRESSED)
                && !_input.GetButton(EMouseButton.ISMIDDLEBUTTONSPRESSED)
                && !_input.GetButton(EMouseButton.ISRIGHTBUTTONPRESSED))
                _direction += 5 * _camera.Transform.Forward * _input.GetMouseWheel();
        }

        private void HeightTransformMovement()
        {
            float input = 0;

            if (_input.GetKey(VirtualKey.E)) input = 1;
            if (_input.GetKey(VirtualKey.Q)) input = -1;
            if (_input.GetKey(VirtualKey.E) && _input.GetKey(VirtualKey.W)) input = 1;
            if (_input.GetKey(VirtualKey.Q) && _input.GetKey(VirtualKey.W)) input = -1;
            if (_input.GetKey(VirtualKey.E) && _input.GetKey(VirtualKey.S)) input = 1;
            if (_input.GetKey(VirtualKey.Q) && _input.GetKey(VirtualKey.S)) input = -1;

            if (_input.GetKey(VirtualKey.W) || _input.GetKey(VirtualKey.S))
                _direction += input * _camera.Transform.LocalUp;
            else
                _direction += input * Vector3.UnitY;
        }
    }
}
