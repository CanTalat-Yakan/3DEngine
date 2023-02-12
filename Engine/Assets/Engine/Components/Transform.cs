using System.Numerics;
using System;
using Vortice.Mathematics;
using Engine.Data;
using Engine.ECS;
using Engine.Helper;

namespace Engine.Components
{
    internal class Transform : Component, IHide
    {
        public Matrix4x4 WorldMatrix = Matrix4x4.Identity;

        public Transform Parent => Entity.Parent is null ? null : Entity.Parent.Transform;

        public Vector3 Forward => Parent is null ? LocalForward : LocalForward + Parent.Forward;
        public Vector3 LocalForward = Vector3.UnitZ;

        public Vector3 Right => Parent is null ? LocalRight : LocalRight + Parent.Right;
        public Vector3 LocalRight = Vector3.UnitX;

        public Vector3 Up => Parent is null ? LocalUp : LocalUp + Parent.Up;
        public Vector3 LocalUp = Vector3.UnitY;

        public Vector3 Position => Parent is null ? LocalPosition : LocalPosition + Parent.Position;
        public Vector3 LocalPosition = Vector3.Zero;

        public Quaternion Rotation => Parent is null ? LocalRotation : LocalRotation * Parent.Rotation;
        public Quaternion LocalRotation { get => _localRotation; set => SetQuaternion(value); }
        public Quaternion _localRotation = Quaternion.Identity;

        public Vector3 EulerAngles { get => _eulerAngles; set => SetEulerAngles(value); }
        private Vector3 _eulerAngles = Vector3.Zero;

        public Vector3 Scale => Parent is null ? LocalScale : LocalScale * Parent.Scale;
        public Vector3 LocalScale = Vector3.One;

        private SPerModelConstantBuffer _modelConstantBuffer;

        public override void OnRegister() =>
            // Register the component with the TransformSystem.
            TransformSystem.Register(this);

        public override void OnUpdate()
        {
            // Calculate the forward direction based on the EulerAngles.
            LocalForward = Vector3.Normalize(new(
                MathF.Sin(EulerAngles.Y.ToRadians()) * MathF.Cos(EulerAngles.X.ToRadians()),
                MathF.Sin(-EulerAngles.X.ToRadians()),
                MathF.Cos(EulerAngles.X.ToRadians()) * MathF.Cos(EulerAngles.Y.ToRadians())));
            // Calculate the right direction based on the forward direction.
            LocalRight = Vector3.Normalize(Vector3.Cross(LocalForward, Vector3.UnitY));
            // Calculate the local up direction based on the forward and right direction.
            LocalUp = Vector3.Normalize(Vector3.Cross(LocalRight, LocalForward));

            // Calculate the translation matrix based on the WorldPosition.
            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(Position);
            // Calculate the rotation matrix based on the WorldRotation.
            Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(Rotation);
            // Calculate the scale matrix based on the WorldScale.
            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(Scale);
            
            // Calculate the world matrix as the transposed result of the combination of scale, rotation and translation matrices.
            WorldMatrix = Matrix4x4.Transpose(scaleMatrix * rotationMatrix * translationMatrix);
        }

        public SPerModelConstantBuffer GetConstantBuffer()
        {
            // Set the ModelView matrix in the ModelConstantBuffer to the WorldMatrix.
            _modelConstantBuffer.ModelView = WorldMatrix;

            return _modelConstantBuffer;
        }

        private void SetEulerAngles(Vector3 value)
        {
            // Set the values for the EulerAngles with degrees.
            _eulerAngles = value;

            // Convert the EulerAngles to radians and create a quaternion from Yaw, Pitch and Roll,
            // where X is Pitch for the horizontal rotation, 
            // Y is the Yaw for the vertical rotation and 
            // Z is for the roll rotation.
            _localRotation = Quaternion.CreateFromYawPitchRoll(
                _eulerAngles.Y.ToRadians(),
                _eulerAngles.X.ToRadians(),
                _eulerAngles.Z.ToRadians());
        }

        private void SetQuaternion(Quaternion value)
        {
            // Set the LocalRotation with the quaternion.
            _localRotation = value;

            // Set the EulerAngles with the new euler from the quaternion.
            _eulerAngles = value.ToEuler();
        }

        public override string ToString()
        {
            return $"""
                {LocalPosition}
                {EulerAngles}
                {LocalScale}
                """;
        }
    }
}
