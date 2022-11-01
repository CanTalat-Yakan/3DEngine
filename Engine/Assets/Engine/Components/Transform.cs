using System;
using System.Numerics;
using Vortice.Mathematics;
using Engine.Data;

namespace Engine.Components
{
    internal class Transform
    {
        internal Transform parent;

        internal SPerModelConstantBuffer m_ConstantsBuffer { get => new SPerModelConstantBuffer() { ModelView = worldMatrix }; }
        internal Matrix4x4 worldMatrix = Matrix4x4.Identity;
        internal Matrix4x4 normalMatrix = Matrix4x4.Identity;
        internal Vector3 position = Vector3.Zero;
        internal Quaternion rotation = Quaternion.Identity;
        internal Vector3 eulerAngles = Vector3.Zero;
        internal Vector3 scale = Vector3.One;

        internal Vector3 forward;
        internal Vector3 right;
        internal Vector3 localUp;

        
        internal void Update()
        {
            forward = Vector3.Normalize(new Vector3(
                MathF.Sin(MathHelper.ToRadians(eulerAngles.Y)) * MathF.Cos(MathHelper.ToRadians(eulerAngles.X)),
                MathF.Sin(MathHelper.ToRadians(-eulerAngles.X)),
                MathF.Cos(MathHelper.ToRadians(eulerAngles.X)) * MathF.Cos(MathHelper.ToRadians(eulerAngles.Y))));

            right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
            localUp = Vector3.Normalize(Vector3.Cross(right, forward));

            this.rotation = Quaternion.CreateFromYawPitchRoll(eulerAngles.X, eulerAngles.Y, eulerAngles.Z);

            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(position);
            Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(rotation);
            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(scale);

            worldMatrix = Matrix4x4.Transpose(scaleMatrix * rotationMatrix * translationMatrix);
            if (parent != null) worldMatrix = worldMatrix * parent.worldMatrix;
        }

        public override string ToString()
        {
            string s = "";
            s += position.ToString() + "\n";
            s += eulerAngles.ToString() + "\n";
            s += scale.ToString();
            return s;
        }
    }
}
