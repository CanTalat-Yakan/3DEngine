using System;
using System.Numerics;
using Windows.System;
using WinUI3DEngine.Assets.Engine.Components;
using WinUI3DEngine.Assets.Engine.Utilities;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace WinUI3DEngine.Assets.Engine.Editor
{
    internal class CController
    {
        internal string m_Profile { get => m_camera.m_Transform.ToString(); }

        CCamera m_camera;
        CInput m_input;

        internal static float m_MovementSpeed = 2;
        float m_rotationSpeed = 5;
        Vector3 m_direction;
        Vector3 m_rotation;


        public CController(CCamera _camera)
        {
            m_camera = _camera;
            m_input = CInput.Instance;
        }

        internal void Update()
        {
            MovementSpeedCalc();

            if (m_input.GetButton(EMouseButton.IsMiddleButtonPressed))
                if (m_input.SetPointerInBounds())
                    ScreenMovement();

            if (m_input.GetButton(EMouseButton.IsRightButtonPressed))
                if (m_input.SetPointerInBounds())
                {
                    TransformMovement();
                    CameraMovement();
                    HeightTransformMovement();
                }

            ScrollMovement();

            m_camera.m_Transform.m_Position += m_direction * (float)CTime.m_Delta * m_MovementSpeed;
            m_camera.m_Transform.m_EulerAngles -= m_rotation * (float)CTime.m_Delta * m_rotationSpeed;

            m_rotation = new Vector3();
            m_direction = new Vector3();
        }

        void MovementSpeedCalc()
        {
            if (m_input.GetButton(EMouseButton.IsLeftButtonPressed)
                || m_input.GetButton(EMouseButton.IsRightButtonPressed))
                m_MovementSpeed += m_input.GetMouseWheel();

            m_MovementSpeed = Math.Clamp(m_MovementSpeed, 0.1f, 10);
        }
                
        void CameraMovement(int _horizontalFactor = 1, int _verticalFactor = 1) =>
            m_rotation = new Vector3(m_input.GetMouseAxis().Y, m_input.GetMouseAxis().X * -1, 0);

        void TransformMovement() =>
            m_direction = m_camera.m_Transform.Forward * m_input.GetAxis().Y + m_camera.m_Transform.Right * m_input.GetAxis().X;

        void ScreenMovement() =>
            m_direction -= m_camera.m_Transform.Right * m_input.GetMouseAxis().X * (float)CTime.m_Delta + m_camera.m_Transform.LocalUp * m_input.GetMouseAxis().Y * (float)CTime.m_Delta;

        void ScrollMovement()
        {
            if (!m_input.GetButton(EMouseButton.IsLeftButtonPressed)
                && !m_input.GetButton(EMouseButton.IsMiddleButtonPressed)
                && !m_input.GetButton(EMouseButton.IsRightButtonPressed))
                m_direction += 5 * m_camera.m_Transform.Forward * m_input.GetMouseWheel();
        }

        void HeightTransformMovement()
        {
            float input = 0;

            if (m_input.GetKey(VirtualKey.E)) input = 1;
            if (m_input.GetKey(VirtualKey.Q)) input = -1;
            if (m_input.GetKey(VirtualKey.E) && m_input.GetKey(VirtualKey.W)) input = 1;
            if (m_input.GetKey(VirtualKey.Q) && m_input.GetKey(VirtualKey.W)) input = -1;
            if (m_input.GetKey(VirtualKey.E) && m_input.GetKey(VirtualKey.S)) input = 1;
            if (m_input.GetKey(VirtualKey.Q) && m_input.GetKey(VirtualKey.S)) input = -1;

            if (m_input.GetKey(VirtualKey.W) || m_input.GetKey(VirtualKey.S))
                m_direction += input * m_camera.m_Transform.LocalUp;
            else
                m_direction += input * Vector3.UnitY;
        }
    }
}
