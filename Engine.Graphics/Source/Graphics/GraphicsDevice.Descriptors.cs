using Vortice.Vulkan;

namespace Engine;

// Descriptor pool, sets, and per-frame UBO bindings for Vulkan backend.
public sealed unsafe partial class GraphicsDevice
{
    private VkDescriptorPool _descriptorPool;
    private VkDescriptorSetLayout _cameraSetLayout;

    private sealed class VulkanDescriptorSet : IDescriptorSet
    {
        private readonly GraphicsDevice _device;
        internal VkDescriptorSet Handle;

        public VulkanDescriptorSet(GraphicsDevice device, VkDescriptorSet handle)
        {
            _device = device;
            Handle = handle;
        }

        public void Dispose()
        {
            // Individual descriptor sets are implicitly freed when the pool is destroyed.
            Handle = default;
        }
    }

    private void CreateDescriptorResources()
    {
        // Simple layout: binding 0 = uniform buffer, binding 1 = combined image sampler.
        VkDescriptorSetLayoutBinding* bindings = stackalloc VkDescriptorSetLayoutBinding[2];
        bindings[0] = new VkDescriptorSetLayoutBinding
        {
            binding = 0,
            descriptorType = VkDescriptorType.UniformBuffer,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.Vertex
        };
        bindings[1] = new VkDescriptorSetLayoutBinding
        {
            binding = 1,
            descriptorType = VkDescriptorType.CombinedImageSampler,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.Fragment
        };

        VkDescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            bindingCount = 2,
            pBindings = bindings
        };

        _deviceApi.vkCreateDescriptorSetLayout(_device, &layoutInfo, null, out _cameraSetLayout).CheckResult();

        VkDescriptorPoolSize* poolSizes = stackalloc VkDescriptorPoolSize[2];
        poolSizes[0] = new VkDescriptorPoolSize(VkDescriptorType.UniformBuffer, 64);
        poolSizes[1] = new VkDescriptorPoolSize(VkDescriptorType.CombinedImageSampler, 64);

        VkDescriptorPoolCreateInfo poolInfo = new()
        {
            maxSets = 64,
            poolSizeCount = 2,
            pPoolSizes = poolSizes
        };

        _deviceApi.vkCreateDescriptorPool(_device, &poolInfo, null, out _descriptorPool).CheckResult();
    }

    private void DestroyDescriptorResources()
    {
        if (_descriptorPool.Handle != 0)
        {
            _deviceApi.vkDestroyDescriptorPool(_device, _descriptorPool);
            _descriptorPool = default;
        }
        if (_cameraSetLayout.Handle != 0)
        {
            _deviceApi.vkDestroyDescriptorSetLayout(_device, _cameraSetLayout);
            _cameraSetLayout = default;
        }
    }

    public IDescriptorSet CreateDescriptorSet()
    {
        if (_descriptorPool.Handle == 0)
            CreateDescriptorResources();

        VkDescriptorSet set;
        VkDescriptorSetLayout* layouts = stackalloc VkDescriptorSetLayout[1];
        layouts[0] = _cameraSetLayout;

        VkDescriptorSetAllocateInfo allocInfo = new()
        {
            descriptorPool = _descriptorPool,
            descriptorSetCount = 1,
            pSetLayouts = layouts
        };

        _deviceApi.vkAllocateDescriptorSets(_device, &allocInfo, &set).CheckResult();
        return new VulkanDescriptorSet(this, set);
    }

    IDescriptorSet IGraphicsDevice.CreateDescriptorSet() => CreateDescriptorSet();

    // Update a descriptor set with an optional uniform buffer and combined image sampler binding.
    internal void UpdateDescriptorSet(
        IDescriptorSet descriptorSet,
        in UniformBufferBinding? uniformBinding,
        in CombinedImageSamplerBinding? samplerBinding)
    {
        if (descriptorSet is not VulkanDescriptorSet vkSet)
            throw new ArgumentException("Descriptor set was not created by this device.", nameof(descriptorSet));

        VkWriteDescriptorSet* writes = stackalloc VkWriteDescriptorSet[2];
        VkDescriptorBufferInfo* bufferInfos = stackalloc VkDescriptorBufferInfo[1];
        VkDescriptorImageInfo* imageInfos = stackalloc VkDescriptorImageInfo[1];
        int writeCount = 0;

        if (uniformBinding.HasValue)
        {
            var ub = uniformBinding.Value;
            if (ub.Buffer is not GraphicsDevice.VulkanBuffer vkBuffer)
                throw new ArgumentException("Uniform buffer was not created by this device.", nameof(uniformBinding));

            bufferInfos[0] = new VkDescriptorBufferInfo
            {
                buffer = vkBuffer.Buffer,
                offset = ub.Offset,
                range = ub.Size
            };

            writes[writeCount++] = new VkWriteDescriptorSet
            {
                dstSet = vkSet.Handle,
                dstBinding = ub.Binding,
                descriptorCount = 1,
                descriptorType = VkDescriptorType.UniformBuffer,
                pBufferInfo = &bufferInfos[0]
            };
        }

        if (samplerBinding.HasValue)
        {
            var sb = samplerBinding.Value;
            if (sb.ImageView is not GraphicsDevice.VulkanImageView vkView)
                throw new ArgumentException("Image view was not created by this device.", nameof(samplerBinding));
            if (sb.Sampler is not GraphicsDevice.VulkanSampler vkSampler)
                throw new ArgumentException("Sampler was not created by this device.", nameof(samplerBinding));

            imageInfos[0] = new VkDescriptorImageInfo
            {
                imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
                imageView = vkView.View,
                sampler = vkSampler.Sampler
            };

            writes[writeCount++] = new VkWriteDescriptorSet
            {
                dstSet = vkSet.Handle,
                dstBinding = sb.Binding,
                descriptorCount = 1,
                descriptorType = VkDescriptorType.CombinedImageSampler,
                pImageInfo = &imageInfos[0]
            };
        }

        if (writeCount == 0)
            return;

        _deviceApi.vkUpdateDescriptorSets(_device, (uint)writeCount, writes, 0, null);
    }

    void IGraphicsDevice.UpdateDescriptorSet(IDescriptorSet descriptorSet, in UniformBufferBinding? uniformBinding, in CombinedImageSamplerBinding? samplerBinding)
        => UpdateDescriptorSet(descriptorSet, uniformBinding, samplerBinding);
}
