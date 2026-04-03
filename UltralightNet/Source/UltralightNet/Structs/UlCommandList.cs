using System.Runtime.CompilerServices;
using UltralightNet.Platform;

namespace UltralightNet.Structs;

/// <summary>
/// List of commands to execute on the GPU.
/// </summary>
/// <seealso cref="IGpuDriver.UpdateCommandList(UlCommandList)"/>
public unsafe ref struct UlCommandList
{
	/// <summary>
	/// The number of commands in the list.
	/// </summary>
	public uint Size;

	/// <summary>
	/// The raw command list data.
	/// </summary>
	public UlCommand* CommandsPtr;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ReadOnlySpan<UlCommand> AsSpan()
	{
		return new ReadOnlySpan<UlCommand>(CommandsPtr, checked((int)Size));
	}
}
