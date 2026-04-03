using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using UltralightNet.GPUCommon;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace UltralightNet.Vulkan;

internal unsafe struct UniformBufferEntry
{
	public Uniforms data;
	public Uniforms* mappedMemory;
	public DeviceMemory memory;
	public Buffer buffer;
	public DescriptorPool descriptorPool;
	public DescriptorSet descriptorSet;
}

public unsafe partial class VulkanGPUDriver
{
	private const ulong UniformBufferSize = 768;

	private uint currentUniformBufferCount = 0;

	[SkipLocalsInit]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void KeepUniformBufferSizeEnough(uint count)
	{
		if (currentUniformBufferCount >= count) return;

		if (uniformBuffer.Handle is not 0)
		{
			vk.DestroyBuffer(device, uniformBuffer, null);
			vk.UnmapMemory(device, uniformBufferMemory);
			vk.FreeMemory(device, uniformBufferMemory, null);
		}

		CreateBuffer(UniformBufferSize * count, BufferUsageFlags.BufferUsageUniformBufferBit, MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit, out uniformBuffer, out uniformBufferMemory);

		fixed (Uniforms** m = &uniforms)
		{
			vk.MapMemory(device, uniformBufferMemory, 0, UniformBufferSize * count, 0, (void**)m);
		}

		var bufferInfo = new DescriptorBufferInfo()
		{
			Buffer = uniformBuffer,
			Offset = 0,
			Range = UniformBufferSize
		};
		var uniformWriteDescriptorSet = new WriteDescriptorSet()
		{
			SType = StructureType.WriteDescriptorSet,
			DstSet = uniformSet,
			DstBinding = 0,
			DstArrayElement = 0,
			DescriptorType = DescriptorType.UniformBufferDynamic,
			DescriptorCount = 1,
			PBufferInfo = &bufferInfo,
		};
		vk.UpdateDescriptorSets(device, 1, &uniformWriteDescriptorSet, 0, null);
		currentUniformBufferCount = count;
	}
}
