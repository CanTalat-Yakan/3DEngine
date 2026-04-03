using UltralightNet.Structs;

namespace UltralightNet.Enums;

/// <summary>
/// The types of commands.
///
/// This enumeration describes the type of command to execute on the GPU.
/// </summary>
/// <seealso cref="UlCommand"/>
public enum CommandType : byte
{
	/// <summary>
	/// Clear the specified render buffer.
	/// </summary>
	ClearRenderBuffer,
	/// <summary>
	/// Draw the specified geometry to the specified render buffer.
	/// </summary>
	DrawGeometry
}
