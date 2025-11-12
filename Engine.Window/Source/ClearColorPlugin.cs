using System.Numerics;

namespace Engine;
/// <summary>Ensures a default ClearColor resource exists.</summary>
public sealed class ClearColorPlugin : IPlugin
{
    /// <summary>Inserts a default ClearColor resource if missing.</summary>
    public void Build(App app)
    {
        if (!app.World.ContainsResource<ClearColor>())
            app.World.InsertResource(new ClearColor(new Vector4(0.45f, 0.55f, 0.60f, 1.00f)));
    }
}
/// <summary>RGBA clear color used for SDL renderer background.</summary>
public readonly struct ClearColor
{
    /// <summary>RGBA color as Vector4 (0..1).</summary>
    public readonly Vector4 Value;

    public ClearColor(Vector4 value)
    {
        Value = value;
    }
}