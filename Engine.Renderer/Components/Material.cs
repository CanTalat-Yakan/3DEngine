using System.Numerics;

namespace Engine;

/// <summary>Simple material with RGBA albedo color.</summary>
public struct Material
{
    public Vector4 Albedo;
    public Material(Vector4 albedo) { Albedo = albedo; }
}