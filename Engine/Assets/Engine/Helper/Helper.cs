using System.Numerics;

namespace Engine.Helper
{
    internal class Helper
    {
    }

    internal static class ExtensionMethods
    {
        public static float ToDegrees(this float value)
        {
            return Vortice.Mathematics.MathHelper.ToDegrees(value);
        }
        
        public static float ToRadians(this float value)
        {
            return Vortice.Mathematics.MathHelper.ToRadians(value);
        }

        public static Vector3 ToDegrees(this Vector3 vector)
        {
            return new(vector.X.ToDegrees(), vector.Y.ToDegrees(), vector.Z.ToDegrees());
        }

        public static Vector3 ToRadians(this Vector3 vector)
        {
            return new(vector.X.ToRadians(), vector.Y.ToRadians(), vector.Z.ToRadians());
        }

        public static bool IsNaN(this Vector3 vector3)
        {
            return float.IsNaN(vector3.X) || float.IsNaN(vector3.Y) || float.IsNaN(vector3.Z);
        }
    }
}
