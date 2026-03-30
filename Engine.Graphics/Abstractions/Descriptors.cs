namespace Engine;

// Descriptor set abstractions

public interface IDescriptorSet : IDisposable { }

public readonly record struct UniformBufferBinding(IBuffer Buffer, uint Binding, ulong Offset, ulong Size);
public readonly record struct CombinedImageSamplerBinding(IImageView ImageView, ISampler Sampler, uint Binding);

