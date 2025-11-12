namespace Engine;

/// <summary>Simple perspective camera marker with projection parameters.</summary>
public struct Camera
{
    public float FovY; // radians
    public float Near;
    public float Far;
    /// <summary>Optional name of a render texture target; if null/empty, renders to the primary surface.</summary>
    public string? TargetName;
    public Camera(float fovY = 60f * (float)(Math.PI / 180.0), float near = 0.1f, float far = 1000f, string? targetName = null)
    { FovY = fovY; Near = near; Far = far; TargetName = targetName; }
}