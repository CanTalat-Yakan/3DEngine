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
            // Register the script with the TransformSystem.
            TransformSystem.Register(this);

        public override void OnUpdate()
        {
            // Set the parent of the transform to the parent transform if it exists.
            if (Entity.Parent != null)
                Parent = Entity.Parent.Transform;

            // Calculate the rotation based on the Euler angles.
            Rotation = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(EulerAngles.X), MathHelper.ToRadians(EulerAngles.Y), MathHelper.ToRadians(EulerAngles.Z));

            // Calculate the forward direction based on the Euler angles.
            Forward = Vector3.Normalize(new(
            MathF.Sin(MathHelper.ToRadians(EulerAngles.Y)) * MathF.Cos(MathHelper.ToRadians(EulerAngles.X)),
            MathF.Sin(MathHelper.ToRadians(-EulerAngles.X)),
            MathF.Cos(MathHelper.ToRadians(EulerAngles.X)) * MathF.Cos(MathHelper.ToRadians(EulerAngles.Y))));

            // Calculate the right direction based on the forward direction.
            Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
            // Calculate the local up direction based on the forward and right direction.
            LocalUp = Vector3.Normalize(Vector3.Cross(Right, Forward));

            // Calculate the translation matrix based on the world position.
            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(WorldPosition);
            // Calculate the rotation matrix based on the world rotation.
            Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(WorldRotation);
            // Calculate the scale matrix based on the world scale.
            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(WorldScale);

            // Calculate the world matrix as the transposed result of the combination of scale, rotation and translation matrices.
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
