namespace UltralightNet.Enums;

public enum FontHinting : byte // CAPI_Defines.h - no type, platform/Config.h - uint8_t
{
	/// <summary>
	///     Lighter hinting algorithm - glyphs are slightly fuzzier but better resemble their original shape. This is
	///     achieved by snapping glyphs to the pixel grid only vertically which better preserves inter-glyph spacing.
	/// </summary>
	Smooth,

	/// <summary>Default hinting algorithm - offers a good balance between sharpness and shape at smaller font sizes.</summary>
	Normal,

	/// <summary>
	///     Strongest hinting algorithm - outputs only black/white glyphs. The result is usually unpleasant if the
	///     underlying TTF does not contain hints for this type of rendering.
	/// </summary>
	Monochrome,
	/// <summary>
	/// No hinting is performed - fonts may be blurry at smaller font sizes.
	/// </summary>
	None
}
