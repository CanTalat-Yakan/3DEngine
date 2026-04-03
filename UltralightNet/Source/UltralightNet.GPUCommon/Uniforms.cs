using System.Numerics;
using System.Runtime.InteropServices;

namespace UltralightNet.GPUCommon;

/// <summary>
///     Uniform Buffer data
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 768)]
public struct Uniforms
{
	public Vector4 State;
	public Matrix4x4 Transform;
	public Vector4 Scalar4_0;
	public Vector4 Scalar4_1;
	public Vector4 Vector_0;
	public Vector4 Vector_1;
	public Vector4 Vector_2;
	public Vector4 Vector_3;
	public Vector4 Vector_4;
	public Vector4 Vector_5;
	public Vector4 Vector_6;
	public Vector4 Vector_7;
	public Matrix4x4 Clip_0;
	public Matrix4x4 Clip_1;
	public Matrix4x4 Clip_2;
	public Matrix4x4 Clip_3;
	public Matrix4x4 Clip_4;
	public Matrix4x4 Clip_5;
	public Matrix4x4 Clip_6;
	public Matrix4x4 Clip_7;
	public uint ClipSize;
}
