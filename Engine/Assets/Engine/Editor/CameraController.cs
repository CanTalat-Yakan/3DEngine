using System.Numerics;
using System;
using Windows.System;
using Engine.ECS;
using Engine.Utilities;

namespace Engine.Editor
{
    internal class CameraController : EditorComponent
    {
        public static float s_MovementSpeed = 2;

        public string Profile { get => Entity.Transform.ToString(); }

        private Input _input;

        private float _rotationSpeed = 5;
        private Vector3 _direction;
        private Vector3 _rotation;

        public CameraController()
        {
            EditorScriptSystem.Register(this);

            _input = Input.Instance;
        }

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

            Entity.Transform.Position += _direction * (float)Time.s_Delta * s_MovementSpeed;
            Entity.Transform.EulerAngles -= _rotation * (float)Time.s_Delta * _rotationSpeed;

            Entity.Transform.EulerAngles.X = Math.Clamp(Entity.Transform.EulerAngles.X, -89, 89);

            _rotation = new();
            _direction = new();
        }

        private void MovementSpeedCalc()
        {
            if (_input.GetButton(EMouseButton.IsRightButtonPressed)
                || _input.GetButton(EMouseButton.IsRightButtonPressed))
                s_MovementSpeed += _input.GetMouseWheel();

            s_MovementSpeed = Math.Clamp(s_MovementSpeed, 0.1f, 10);
        }

        private void CameraMovement(int _horizontalFactor = 1, int _verticalFactor = 1) =>
            _rotation = new(_input.GetMouseAxis().Y, _input.GetMouseAxis().X, 0);

        private void TransformMovement() =>
            _direction = Entity.Transform.Forward * _input.GetAxis().Y + Entity.Transform.Right * _input.GetAxis().X;

        private void ScreenMovement() =>
            _direction -=   Entity.Transform.Right * _input.GetMouseAxis().X * (float)Time.s_Delta + Entity.Transform.LocalUp * _input.GetMouseAxis().Y * (float)Time.s_Delta;

        private void ScrollMovement()
        {
            if (!_input.GetButton(EMouseButton.IsRightButtonPressed)
                && !_input.GetButton(EMouseButton.IsMiddleButtonPressed)
                && !_input.GetButton(EMouseButton.IsRightButtonPressed))
                _direction += 5 * Entity.Transform.Forward * _input.GetMouseWheel();
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
                _direction += input * Entity.Transform.LocalUp;
            else
                _direction += input * Vector3.UnitY;
        }
    }
}
