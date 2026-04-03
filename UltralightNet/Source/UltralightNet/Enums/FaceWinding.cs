namespace UltralightNet.Enums;

/// <summary>
/// The winding order for front-facing triangles. (Only used when the GPU renderer is used)
/// </summary>
public enum FaceWinding : byte // CAPI_Defines.h - no type, platform/Config.h - uint8_t
{
	/// <summary>Clockwise Winding (Direct3D, etc.)</summary>
	Clockwise,

	/// <summary>Counter-Clockwise Winding (OpenGL, etc.)</summary>
	CounterClockwise
}
