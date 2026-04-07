using System.Numerics;

namespace Engine;

/// <summary>Immutable camera snapshot for rendering, containing view/projection matrices and viewport dimensions.</summary>
/// <seealso cref="RenderCameras"/>
/// <seealso cref="CameraUniform"/>
public readonly struct RenderCamera
{
    /// <summary>The camera's view (world-to-eye) matrix.</summary>
    public readonly Matrix4x4 View;

    /// <summary>The camera's projection (eye-to-clip) matrix.</summary>
    public readonly Matrix4x4 Projection;

    /// <summary>Viewport width in pixels.</summary>
    public readonly int Width;

    /// <summary>Viewport height in pixels.</summary>
    public readonly int Height;

    /// <summary>Creates a new <see cref="RenderCamera"/> from the specified matrices and viewport size.</summary>
    /// <param name="view">The view matrix.</param>
    /// <param name="projection">The projection matrix.</param>
    /// <param name="width">Viewport width in pixels.</param>
    /// <param name="height">Viewport height in pixels.</param>
    public RenderCamera(Matrix4x4 view, Matrix4x4 projection, int width, int height)
    {
        View = view;
        Projection = projection;
        Width = width;
        Height = height;
    }
}

/// <summary>Collection of active render cameras. Typically one per viewport.</summary>
/// <seealso cref="RenderCamera"/>
public sealed class RenderCameras
{
    /// <summary>The list of active cameras to render from.</summary>
    public List<RenderCamera> Items { get; } = new();
}

/// <summary>GPU uniform buffer layout for camera data (view + projection matrices).</summary>
/// <seealso cref="RenderCamera"/>
public struct CameraUniform
{
    /// <summary>The view (world-to-eye) matrix.</summary>
    public Matrix4x4 View;

    /// <summary>The projection (eye-to-clip) matrix.</summary>
    public Matrix4x4 Projection;
}