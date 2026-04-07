namespace Engine;

/// <summary>Descriptor set abstractions for binding uniform buffers and samplers to shaders.</summary>

/// <summary>Handle to an allocated descriptor set for binding resources to shaders.</summary>
public interface IDescriptorSet : IDisposable { }

/// <summary>Binding descriptor for a uniform buffer within a descriptor set.</summary>
/// <param name="Buffer">The uniform buffer to bind.</param>
/// <param name="Binding">Shader binding slot index.</param>
/// <param name="Offset">Byte offset into the buffer.</param>
/// <param name="Size">Byte size of the bound range.</param>
public readonly record struct UniformBufferBinding(IBuffer Buffer, uint Binding, ulong Offset, ulong Size);

/// <summary>Binding descriptor for a combined image sampler within a descriptor set.</summary>
/// <param name="ImageView">The image view providing the texture data.</param>
/// <param name="Sampler">The sampler defining filtering and addressing modes.</param>
/// <param name="Binding">Shader binding slot index.</param>
public readonly record struct CombinedImageSamplerBinding(IImageView ImageView, ISampler Sampler, uint Binding);
