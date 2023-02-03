using System.Numerics;
using System;
using Vortice.Mathematics;
using Engine.Data;
using Engine.ECS;

namespace Engine.Components
{
    internal class Transform : Component
    {
        public SPerModelConstantBuffer ConstantsBuffer { get => new() { ModelView = WorldMatrix }; }

        public Transform Parent;

        public Vector3 Forward { get; private set; }
        public Vector3 Right { get; private set; }
        public Vector3 LocalUp { get; private set; }

        public Matrix4x4 WorldMatrix = Matrix4x4.Identity;
        public Matrix4x4 NormalMatrix = Matrix4x4.Identity;

        public Vector3 WorldPosition { get => Parent is null ? Position : Position + Parent.WorldPosition; }
        public Vector3 Position = Vector3.Zero;

        public Quaternion WorldRotation { get => Parent is null ? Rotation : Rotation * Parent.WorldRotation; }
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 EulerAngles = Vector3.Zero;

        public Vector3 WorldScale { get => Parent is null ? Scale : Scale * Parent.WorldScale; }
        public Vector3 Scale = Vector3.One;

        public override void OnRegister() =>
            TransformSystem.Register(this);

        public override void OnUpdate()
        {
            if (Entity.Parent != null)
                Parent = Entity.Parent.Transform;

            Rotation = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(EulerAngles.X), MathHelper.ToRadians(EulerAngles.Y), MathHelper.ToRadians(EulerAngles.Z));

            Forward = Vector3.Normalize(new(
                MathF.Sin(MathHelper.ToRadians(EulerAngles.Y)) * MathF.Cos(MathHelper.ToRadians(EulerAngles.X)),
                MathF.Sin(MathHelper.ToRadians(-EulerAngles.X)),
                MathF.Cos(MathHelper.ToRadians(EulerAngles.X)) * MathF.Cos(MathHelper.ToRadians(EulerAngles.Y))));

            Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
            LocalUp = Vector3.Normalize(Vector3.Cross(Right, Forward));

            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(WorldPosition);
            Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(WorldRotation);
            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(WorldScale);

            WorldMatrix = Matrix4x4.Transpose(scaleMatrix * rotationMatrix * translationMatrix);
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
