using System.Numerics;

namespace Engine;

/// <summary>Simple material component with RGBA albedo color.</summary>
/// <seealso cref="Mesh"/>
/// <seealso cref="Transform"/>
public struct Material
{
    /// <summary>Base albedo color as RGBA (0–1 per channel).</summary>
    public Vector4 Albedo;

    /// <summary>Creates a material with the specified albedo color.</summary>
    /// <param name="albedo">RGBA albedo color.</param>
    public Material(Vector4 albedo)
    {
        Albedo = albedo;
    }
}