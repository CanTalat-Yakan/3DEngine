using System.Numerics;
using System;
using Vortice.Mathematics;
using Engine.Data;
using Engine.ECS;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Engine.Components
{
    internal class Transform : Component
    {
        public Transform Parent;

        public SPerModelConstantBuffer ConstantsBuffer { get => new() { ModelView = WorldMatrix }; }

        public Vector3 Forward { get; private set; }
        public Vector3 Right { get; private set; }
        public Vector3 LocalUp { get; private set; }

        public Matrix4x4 WorldMatrix = Matrix4x4.Identity;
        public Matrix4x4 NormalMatrix = Matrix4x4.Identity;
        public Vector3 Position = Vector3.Zero;
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 EulerAngles = Vector3.Zero;
        public Vector3 Scale = Vector3.One;

        public override void Register() => TransformSystem.Register(this);
        
        public override void Update()
        {
            if (Entity.Parent != null)
                Parent = Entity.Parent.Transform;

            //EulerAngles = Rotation.ToEuler();
            Rotation = Quaternion.CreateFromYawPitchRoll(EulerAngles.X, EulerAngles.Y, EulerAngles.Z);

            Forward = Vector3.Normalize(new(
                MathF.Sin(MathHelper.ToRadians(EulerAngles.Y)) * MathF.Cos(MathHelper.ToRadians(EulerAngles.X)),
                MathF.Sin(MathHelper.ToRadians(-EulerAngles.X)),
                MathF.Cos(MathHelper.ToRadians(EulerAngles.X)) * MathF.Cos(MathHelper.ToRadians(EulerAngles.Y))));

            Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
            LocalUp = Vector3.Normalize(Vector3.Cross(Right, Forward));

            WorldMatrix = CalculateWorldMatrix(Position, EulerAngles, Scale, Parent);
        }

        Matrix4x4 CalculateWorldMatrix(Vector3 position, Vector3 rotation, Vector3 scale, Transform parent)
        {
            while (parent != null)
            {
                position += parent.Position;
                rotation += parent.EulerAngles;
                scale *= parent.Scale;

                parent = parent.Parent;
            }

            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(position);
            Matrix4x4 rotationMatrix = Matrix4x4.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z); //.CreateFromQuaternion(rotation);
            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(scale);

            return Matrix4x4.Transpose(scaleMatrix * rotationMatrix * translationMatrix);
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
