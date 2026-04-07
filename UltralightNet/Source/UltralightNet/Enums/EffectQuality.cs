namespace UltralightNet.Enums;

/// <summary>
/// The winding order for front-facing triangles. (Only used when the GPU renderer is used)
/// </summary>
public enum EffectQuality : byte // CAPI_Defines.h - no type, platform/Config.h - uint8_t
{
	/// <summary>
	/// Fastest effect quality - uses the lowest quality effects (half-resolution, fewer passes, etc.)
	/// </summary>
	Low,
	/// <summary>
	/// Default effect quality - strikes a good balance between quality and performance.
	/// </summary>
	Medium,
	/// <summary>
	/// Highest effect quality - favors quality over performance.
	/// </summary>
	High,
}
