using System.Numerics;
using System;
using Windows.System;
using Engine.ECS;
using Engine.Utilities;
using Editor.Controller;
using Engine.Components;

namespace Engine.Editor
{
    internal class CameraController : EditorComponent
    {
        public string Profile { get => Entity.Transform.ToString(); }

        private static float s_movementSpeed = 2;
        public static float MovementSpeed { get => s_movementSpeed; set => s_movementSpeed = value; }

        private float _rotationSpeed = 5;
        private Vector3 _direction;
        private Vector3 _rotation;

        public override void OnRegister() =>
            EditorScriptSystem.Register(this);

        public override void OnUpdate()
        {
            MovementSpeedCalc();

            if (Input.GetButton(EMouseButton.IsMiddleButtonPressed))
                if (Input.SetPointerInBounds())
                    ScreenMovement();

            if (Input.GetButton(EMouseButton.IsRightButtonPressed))
                if (Input.SetPointerInBounds())
                {
                    TransformMovement();
                    CameraMovement();
                    HeightTransformMovement();
                }

            ScrollMovement();

            Entity.Transform.Position += _direction * (float)Time.Delta * s_movementSpeed;
            Entity.Transform.EulerAngles -= _rotation * (float)Time.Delta * _rotationSpeed;

            Entity.Transform.EulerAngles.X = Math.Clamp(Entity.Transform.EulerAngles.X, -89, 89);

            _rotation = new();
            _direction = new();
        }

        private void MovementSpeedCalc()
        {
            if (Input.GetButton(EMouseButton.IsRightButtonPressed)
                || Input.GetButton(EMouseButton.IsRightButtonPressed))
                s_movementSpeed += Input.GetMouseWheel();

            s_movementSpeed = Math.Clamp(s_movementSpeed, 0.1f, 10);
        }

        private void CameraMovement(int _horizontalFactor = 1, int _verticalFactor = 1) =>
            _rotation = new(Input.GetMouseAxis().Y, Input.GetMouseAxis().X, 0);

        private void TransformMovement() =>
            _direction = Entity.Transform.Forward * Input.GetAxis().Y + Entity.Transform.Right * Input.GetAxis().X;

        private void ScreenMovement() =>
            _direction -= Entity.Transform.Right * Input.GetMouseAxis().X * (float)Time.Delta + Entity.Transform.LocalUp * Input.GetMouseAxis().Y * (float)Time.Delta;

        private void ScrollMovement()
        {
            if (!Input.GetButton(EMouseButton.IsRightButtonPressed)
                && !Input.GetButton(EMouseButton.IsMiddleButtonPressed)
                && !Input.GetButton(EMouseButton.IsRightButtonPressed))
                _direction += 5 * Entity.Transform.Forward * Input.GetMouseWheel();
        }

        private void HeightTransformMovement()
        {
            float input = 0;

            if (Input.GetKey(VirtualKey.E)) input = 1;
            if (Input.GetKey(VirtualKey.Q)) input = -1;
            if (Input.GetKey(VirtualKey.E) && Input.GetKey(VirtualKey.W)) input = 1;
            if (Input.GetKey(VirtualKey.Q) && Input.GetKey(VirtualKey.W)) input = -1;
            if (Input.GetKey(VirtualKey.E) && Input.GetKey(VirtualKey.S)) input = 1;
            if (Input.GetKey(VirtualKey.Q) && Input.GetKey(VirtualKey.S)) input = -1;
            
            if (Input.GetKey(VirtualKey.W) || Input.GetKey(VirtualKey.S))
                _direction += input * Entity.Transform.LocalUp;
            else
                _direction += input * Vector3.UnitY;
        }
    }
}
