using UltralightNet.Structs;

namespace UltralightNet.Enums;

/// <summary>
/// Shader program types, used with <see cref="UlGpuState.ShaderType"/>.
///
/// Each of these correspond to a vertex/pixel shader pair. You can find stock shader code for these
/// in the 'shaders' folder of the AppCore repo.
/// </summary>
public enum ShaderType : byte
{
	/// <summary>
	/// Shader program for filling quad geometry.
	/// </summary>
	Fill,
	/// <summary>
	/// Shader program for filling tessellated path geometry.
	/// </summary>
	FillPath
}
