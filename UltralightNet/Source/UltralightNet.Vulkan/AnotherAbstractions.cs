using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace UltralightNet.Vulkan;

public unsafe partial class VulkanGPUDriver
{
	private readonly byte* _shader_main;

	private PipelineShaderStageCreateInfo LoadShader(string name, ShaderStageFlags stage)
	{
		var stream = typeof(VulkanGPUDriver).Assembly.GetManifestResourceStream(name);
		byte* bytesPtr = (byte*)Marshal.AllocHGlobal((int)stream!.Length);
		stream.Read(new Span<byte>(bytesPtr, (int)stream.Length));
		var shaderModule = CreateShaderModule(bytesPtr, (nuint)stream.Length);
		Marshal.FreeHGlobal((IntPtr)bytesPtr);

		return new()
		{
			SType = StructureType.PipelineShaderStageCreateInfo,
			Stage = stage,
			Module = shaderModule,
			PName = _shader_main
		};
	}
}
