using System.Numerics;
using System;
using Windows.System;
using Engine.ECS;
using Engine.Utilities;

namespace Engine.Editor
{
    internal class CameraController : EditorComponent
    {
        public string Profile { get => _entity.Transform.ToString(); }

        public static float MovementSpeed { get => s_movementSpeed; set => s_movementSpeed = value; }
        private static float s_movementSpeed = 2;

        private Input _input { get => Input.Instance; }

        private float _rotationSpeed = 5;
        private Vector3 _direction;
        private Vector3 _rotation;

        public override void Register() => 
            EditorScriptSystem.Register(this);

        public override void Update()
        {
            MovementSpeedCalc();

            if (_input.GetButton(EMouseButton.IsMiddleButtonPressed))
                if (_input.SetPointerInBounds())
                    ScreenMovement();

            if (_input.GetButton(EMouseButton.IsRightButtonPressed))
                if (_input.SetPointerInBounds())
                {
                    TransformMovement();
                    CameraMovement();
                    HeightTransformMovement();
                }

            ScrollMovement();

            _entity.Transform.Position += _direction * (float)Time.Delta * s_movementSpeed;
            _entity.Transform.EulerAngles -= _rotation * (float)Time.Delta * _rotationSpeed;

            _entity.Transform.EulerAngles.X = Math.Clamp(_entity.Transform.EulerAngles.X, -89, 89);

            _rotation = new();
            _direction = new();
        }

        private void MovementSpeedCalc()
        {
            if (_input.GetButton(EMouseButton.IsRightButtonPressed)
                || _input.GetButton(EMouseButton.IsRightButtonPressed))
                s_movementSpeed += _input.GetMouseWheel();

            s_movementSpeed = Math.Clamp(s_movementSpeed, 0.1f, 10);
        }

        private void CameraMovement(int _horizontalFactor = 1, int _verticalFactor = 1) =>
            _rotation = new(_input.GetMouseAxis().Y, _input.GetMouseAxis().X, 0);

        private void TransformMovement() =>
            _direction = _entity.Transform.Forward * _input.GetAxis().Y + _entity.Transform.Right * _input.GetAxis().X;

        private void ScreenMovement() =>
            _direction -= _entity.Transform.Right * _input.GetMouseAxis().X * (float)Time.Delta + _entity.Transform.LocalUp * _input.GetMouseAxis().Y * (float)Time.Delta;

        private void ScrollMovement()
        {
            if (!_input.GetButton(EMouseButton.IsRightButtonPressed)
                && !_input.GetButton(EMouseButton.IsMiddleButtonPressed)
                && !_input.GetButton(EMouseButton.IsRightButtonPressed))
                _direction += 5 * _entity.Transform.Forward * _input.GetMouseWheel();
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
                _direction += input * _entity.Transform.LocalUp;
            else
                _direction += input * Vector3.UnitY;
        }
    }
}
