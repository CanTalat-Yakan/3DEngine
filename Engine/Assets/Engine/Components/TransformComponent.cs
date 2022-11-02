using System;
using System.Numerics;
using Vortice.Mathematics;
using Engine.Data;

namespace Engine.Components
{
    public class TransformComponent
    {
        public TransformComponent Parent;

        public SPerModelConstantBuffer ConstantsBuffer { get => new SPerModelConstantBuffer() { ModelView = WorldMatrix }; }
        public Vector3 Forward { get; private set; }
        public Vector3 Right { get; private set; }
        public Vector3 LocalUp { get; private set; }

        public Matrix4x4 WorldMatrix = Matrix4x4.Identity;
        public Matrix4x4 NormalMatrix = Matrix4x4.Identity;
        public Vector3 Position = Vector3.Zero;
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 EulerAngles = Vector3.Zero;
        public Vector3 Scale = Vector3.One;

        public void Update()
        {
            Forward = Vector3.Normalize(new Vector3(
                MathF.Sin(MathHelper.ToRadians(EulerAngles.Y)) * MathF.Cos(MathHelper.ToRadians(EulerAngles.X)),
                MathF.Sin(MathHelper.ToRadians(-EulerAngles.X)),
                MathF.Cos(MathHelper.ToRadians(EulerAngles.X)) * MathF.Cos(MathHelper.ToRadians(EulerAngles.Y))));

            Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
            LocalUp = Vector3.Normalize(Vector3.Cross(Right, Forward));

            this.Rotation = Quaternion.CreateFromYawPitchRoll(EulerAngles.X, EulerAngles.Y, EulerAngles.Z);

            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(Position);
            Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(Rotation);
            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(Scale);

            WorldMatrix = Matrix4x4.Transpose(scaleMatrix * rotationMatrix * translationMatrix);
            if (Parent != null) WorldMatrix = WorldMatrix * Parent.WorldMatrix;
        }

        public override string ToString()
        {
            string s = "";
            s += Position.ToString() + "\n";
            s += EulerAngles.ToString() + "\n";
            s += Scale.ToString();
            return s;
        }
    }
}
