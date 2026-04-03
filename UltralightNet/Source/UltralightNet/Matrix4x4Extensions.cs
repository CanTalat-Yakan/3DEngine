using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UltralightNet;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class Methods
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[LibraryImport(LibUltralight)]
	public static partial Matrix4x4 ulApplyProjection(Matrix4x4 transform, float viewport_width, float viewport_height,
		[MarshalAs(UnmanagedType.U1)] bool flip_y);
}

// Shut up
// ReSharper disable once InconsistentNaming
public static class Matrix4x4Extensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix4x4 ApplyProjection(in this Matrix4x4 transform, float viewportWidth, float viewportHeight,
		bool flipY)
	{
		return Methods.ulApplyProjection(transform, viewportWidth, viewportHeight, flipY);
	}
}
