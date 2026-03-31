using System.Numerics;

namespace Engine;

public readonly struct RenderCamera
{
    public readonly Matrix4x4 View;
    public readonly Matrix4x4 Projection;
    public readonly int Width;
    public readonly int Height;

    public RenderCamera(Matrix4x4 view, Matrix4x4 projection, int width, int height)
    {
        View = view;
        Projection = projection;
        Width = width;
        Height = height;
    }
}

public sealed class RenderCameras
{
    public List<RenderCamera> Items { get; } = new();
}

/// <summary>GPU uniform buffer layout for camera data.</summary>
public struct CameraUniform
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
}