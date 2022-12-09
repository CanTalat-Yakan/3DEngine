using System.Numerics;
using System;
using Windows.System;
using Engine.Utilities;
using Engine.ECS;

namespace Engine.Editor
{
    internal class CameraController
    {
        public static float s_MovementSpeed = 2;

        public string Profile { get => _cameraEntity.Transform.ToString(); }

        private Entity _cameraEntity;
        private Input _input;

        private float _rotationSpeed = 5;
        private Vector3 _direction;
        private Vector3 _rotation;

        public CameraController(Entity cameraEntity)
        {
            _cameraEntity = cameraEntity;
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

            _cameraEntity.Transform.Position += _direction * (float)Time.s_Delta * s_MovementSpeed;
            _cameraEntity.Transform.EulerAngles -= _rotation * (float)Time.s_Delta * _rotationSpeed;

            _cameraEntity.Transform.EulerAngles.X = Math.Clamp(_cameraEntity.Transform.EulerAngles.X, -89, 89);

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
            _direction = _cameraEntity.Transform.Forward * _input.GetAxis().Y + _cameraEntity.Transform.Right * _input.GetAxis().X;

        private void ScreenMovement() =>
            _direction -= _cameraEntity.Transform.Right * _input.GetMouseAxis().X * (float)Time.s_Delta + _cameraEntity.Transform.LocalUp * _input.GetMouseAxis().Y * (float)Time.s_Delta;

        private void ScrollMovement()
        {
            if (!_input.GetButton(EMouseButton.ISRIGHTBUTTONPRESSED)
                && !_input.GetButton(EMouseButton.ISMIDDLEBUTTONSPRESSED)
                && !_input.GetButton(EMouseButton.ISRIGHTBUTTONPRESSED))
                _direction += 5 * _cameraEntity.Transform.Forward * _input.GetMouseWheel();
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
                _direction += input * _cameraEntity.Transform.LocalUp;
            else
                _direction += input * Vector3.UnitY;
        }
    }
}
