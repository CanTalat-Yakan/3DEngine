using System;
using System.Numerics;
using Vortice.Mathematics;
using Engine.Data;

namespace Engine.Components
{
    internal class Transform
    {
        internal Transform m_Parent;

        internal SPerModelConstantBuffer m_ConstantsBuffer { get => new SPerModelConstantBuffer() { ModelView = m_WorldMatrix }; }
        internal Matrix4x4 m_WorldMatrix = Matrix4x4.Identity;
        internal Matrix4x4 m_NormalMatrix = Matrix4x4.Identity;
        internal Vector3 m_Position = Vector3.Zero;
        internal Quaternion m_Rotation = Quaternion.Identity;
        internal Vector3 m_EulerAngles = Vector3.Zero;
        internal Vector3 m_Scale = Vector3.One;

        internal Vector3 Forward;
        internal Vector3 Right;
        internal Vector3 LocalUp;

        
        internal void Update()
        {
            Forward = Vector3.Normalize(new Vector3(
                MathF.Sin(MathHelper.ToRadians(m_EulerAngles.Y)) * MathF.Cos(MathHelper.ToRadians(m_EulerAngles.X)),
                MathF.Sin(MathHelper.ToRadians(-m_EulerAngles.X)),
                MathF.Cos(MathHelper.ToRadians(m_EulerAngles.X)) * MathF.Cos(MathHelper.ToRadians(m_EulerAngles.Y))));

            Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
            LocalUp = Vector3.Normalize(Vector3.Cross(Right, Forward));

            m_Rotation = Quaternion.CreateFromYawPitchRoll(m_EulerAngles.X, m_EulerAngles.Y, m_EulerAngles.Z);

            Matrix4x4 translation = Matrix4x4.CreateTranslation(m_Position);
            Matrix4x4 rotation = Matrix4x4.CreateFromQuaternion(m_Rotation);
            Matrix4x4 scale = Matrix4x4.CreateScale(m_Scale);

            m_WorldMatrix = Matrix4x4.Transpose(scale * rotation * translation);
            if (m_Parent != null) m_WorldMatrix = m_WorldMatrix * m_Parent.m_WorldMatrix;
        }

        public override string ToString()
        {
            string s = "";
            s += m_Position.ToString() + "\n";
            s += m_EulerAngles.ToString() + "\n";
            s += m_Scale.ToString();
            return s;
        }
    }
}
