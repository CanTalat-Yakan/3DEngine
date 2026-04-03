using UltralightNet.Enums;

namespace UltralightNet.Structs;

/// <summary>
///  A command to execute on the GPU.
/// <br/>
/// <br/>
/// This structure describes a command to be executed on the GPU.
/// <br/>
/// <br/>
/// Commands are dispatched to the GPU driver asynchronously via GPUDriver::UpdateCommandList(),
/// the GPU driver should consume these commands and execute them at an appropriate time.
/// </summary>
/// <seealso cref="UlCommandList"/>
public struct UlCommand : IEquatable<UlCommand>
{
	private byte _commandType;

	/// <summary>
	/// The type of command to dispatch.
	/// </summary>
	public CommandType CommandType
	{
		readonly get => Methods.BitCast<byte, CommandType>(_commandType);
		set => _commandType = Methods.BitCast<CommandType, byte>(value);
	}

	/// <summary>
	/// The current GPU state.
	/// </summary>
	public UlGpuState GpuState;

	/// <summary>
	/// The geometry ID to bind. (used with CommandType::DrawGeometry)
	/// </summary>
	public uint GeometryId;

	/// <summary>
	/// The number of indices.   (used with CommandType::DrawGeometry)
	/// </summary>
	public uint IndicesCount;

	/// <summary>
	///The index to start from. (used with CommandType::DrawGeometry)
	/// </summary>
	public uint IndicesOffset;

	public readonly bool Equals(UlCommand other)
	{
		return CommandType == other.CommandType && GpuState.Equals(other.GpuState) && GeometryId == other.GeometryId &&
		       IndicesCount == other.IndicesCount && IndicesOffset == other.IndicesOffset;
	}
}
