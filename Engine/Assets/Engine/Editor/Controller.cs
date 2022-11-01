using System;
using System.Numerics;
using Windows.System;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using Engine.Components;
using Engine.Utilities;

namespace Engine.Editor
{
    internal class Controller
    {
        internal string m_Profile { get => camera.transform.ToString(); }

        Camera camera;
        Input input;

        internal static float s_movementSpeed = 2;
        float rotationSpeed = 5;
        Vector3 direction;
        Vector3 rotation;


        public Controller(Camera _camera)
        {
            camera = _camera;
            input = Input.Instance;
        }

        internal void Update()
        {
            MovementSpeedCalc();

            if (input.GetButton(EMouseButton.IsMiddleButtonPressed))
                if (input.SetPointerInBounds())
                    ScreenMovement();

            if (input.GetButton(EMouseButton.IsRightButtonPressed))
                if (input.SetPointerInBounds())
                {
                    TransformMovement();
                    CameraMovement();
                    HeightTransformMovement();
                }

            ScrollMovement();

            camera.transform.position += direction * (float)Time.s_delta * s_movementSpeed;
            camera.transform.eulerAngles -= rotation * (float)Time.s_delta * rotationSpeed;

            rotation = new Vector3();
            direction = new Vector3();
        }

        void MovementSpeedCalc()
        {
            if (input.GetButton(EMouseButton.IsLeftButtonPressed)
                || input.GetButton(EMouseButton.IsRightButtonPressed))
                s_movementSpeed += input.GetMouseWheel();

            s_movementSpeed = Math.Clamp(s_movementSpeed, 0.1f, 10);
        }
                
        void CameraMovement(int _horizontalFactor = 1, int _verticalFactor = 1) =>
            rotation = new Vector3(input.GetMouseAxis().Y, input.GetMouseAxis().X, 0);

        void TransformMovement() =>
            direction = camera.transform.forward * input.GetAxis().Y + camera.transform.right * input.GetAxis().X;

        void ScreenMovement() =>
            direction -= camera.transform.right * input.GetMouseAxis().X * (float)Time.s_delta + camera.transform.localUp * input.GetMouseAxis().Y * (float)Time.s_delta;

        void ScrollMovement()
        {
            if (!input.GetButton(EMouseButton.IsLeftButtonPressed)
                && !input.GetButton(EMouseButton.IsMiddleButtonPressed)
                && !input.GetButton(EMouseButton.IsRightButtonPressed))
                direction += 5 * camera.transform.forward * input.GetMouseWheel();
        }

        void HeightTransformMovement()
        {
            float input = 0;

            if (this.input.GetKey(VirtualKey.E)) input = 1;
            if (this.input.GetKey(VirtualKey.Q)) input = -1;
            if (this.input.GetKey(VirtualKey.E) && this.input.GetKey(VirtualKey.W)) input = 1;
            if (this.input.GetKey(VirtualKey.Q) && this.input.GetKey(VirtualKey.W)) input = -1;
            if (this.input.GetKey(VirtualKey.E) && this.input.GetKey(VirtualKey.S)) input = 1;
            if (this.input.GetKey(VirtualKey.Q) && this.input.GetKey(VirtualKey.S)) input = -1;

            if (this.input.GetKey(VirtualKey.W) || this.input.GetKey(VirtualKey.S))
                direction += input * camera.transform.localUp;
            else
                direction += input * Vector3.UnitY;
        }
    }
}
