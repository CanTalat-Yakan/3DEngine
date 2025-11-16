using System.Text;
using Vortice.Vulkan;

namespace Engine;

// Shader modules and graphics pipeline creation for Vulkan backend.
public sealed unsafe partial class GraphicsDevice
{
    private sealed class VulkanShader : IShader
    {
        private readonly GraphicsDevice _device;
        public ShaderDesc Description { get; }
        internal VkShaderModule Module;

        public VulkanShader(GraphicsDevice device, ShaderDesc desc, VkShaderModule module)
        {
            _device = device;
            Description = desc;
            Module = module;
        }

        public void Dispose()
        {
            if (Module.Handle != 0)
            {
                _device._deviceApi.vkDestroyShaderModule(_device._device, Module);
                Module = default;
            }
        }
    }

    private sealed class VulkanGraphicsPipeline : IPipeline
    {
        private readonly GraphicsDevice _device;
        internal VkPipeline Pipeline;
        internal VkPipelineLayout Layout;

        public VulkanGraphicsPipeline(GraphicsDevice device, VkPipeline pipeline, VkPipelineLayout layout)
        {
            _device = device;
            Pipeline = pipeline;
            Layout = layout;
        }

        public void Dispose()
        {
            if (Pipeline.Handle != 0)
            {
                _device._deviceApi.vkDestroyPipeline(_device._device, Pipeline);
                Pipeline = default;
            }
            if (Layout.Handle != 0)
            {
                _device._deviceApi.vkDestroyPipelineLayout(_device._device, Layout);
                Layout = default;
            }
        }
    }

    public IShader CreateShader(ShaderDesc desc)
    {
        fixed (byte* codePtr = desc.Bytecode.Span)
        {
            VkShaderModuleCreateInfo info = new()
            {
                codeSize = (nuint)desc.Bytecode.Length,
                pCode = (uint*)codePtr
            };

            _deviceApi.vkCreateShaderModule(_device, &info, null, out VkShaderModule module).CheckResult();
            return new VulkanShader(this, desc, module);
        }
    }

    public IPipeline CreateGraphicsPipeline(GraphicsPipelineDesc desc)
    {
        if (desc.RenderPass is not VulkanRenderPass rpWrapper)
            throw new ArgumentException("RenderPass must originate from this GraphicsDevice.", nameof(desc));

        VkRenderPass rpHandle = rpWrapper.Handle;

        var vs = (VulkanShader)desc.VertexShader;
        var fs = (VulkanShader)desc.FragmentShader;

        VkUtf8ReadOnlyString entryName = Encoding.UTF8.GetBytes(desc.VertexShader.Description.EntryPoint);

        VkPipelineShaderStageCreateInfo* stages = stackalloc VkPipelineShaderStageCreateInfo[2];
        stages[0] = new VkPipelineShaderStageCreateInfo
        {
            stage = VkShaderStageFlags.Vertex,
            module = vs.Module,
            pName = entryName
        };
        stages[1] = new VkPipelineShaderStageCreateInfo
        {
            stage = VkShaderStageFlags.Fragment,
            module = fs.Module,
            pName = entryName
        };

        VkPipelineVertexInputStateCreateInfo vertexInput = new();
        VkPipelineInputAssemblyStateCreateInfo inputAssembly = new()
        {
            topology = VkPrimitiveTopology.TriangleList
        };

        VkViewport viewport = new(0, 0, _swapchainExtent.width, _swapchainExtent.height, 0, 1);
        VkRect2D scissor = new(new VkOffset2D(0, 0), _swapchainExtent);

        VkPipelineViewportStateCreateInfo viewportState = new()
        {
            viewportCount = 1,
            pViewports = &viewport,
            scissorCount = 1,
            pScissors = &scissor
        };

        VkPipelineRasterizationStateCreateInfo rasterizer = new()
        {
            polygonMode = VkPolygonMode.Fill,
            cullMode = VkCullModeFlags.Back,
            frontFace = VkFrontFace.CounterClockwise,
            lineWidth = 1.0f
        };

        VkPipelineMultisampleStateCreateInfo multisample = new()
        {
            rasterizationSamples = VkSampleCountFlags.Count1
        };

        VkPipelineColorBlendAttachmentState colorBlendAttachment = new()
        {
            colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A,
            blendEnable = false
        };

        VkPipelineColorBlendStateCreateInfo colorBlend = new()
        {
            attachmentCount = 1,
            pAttachments = &colorBlendAttachment
        };

        VkDynamicState* dynamics = stackalloc VkDynamicState[2];
        dynamics[0] = VkDynamicState.Viewport;
        dynamics[1] = VkDynamicState.Scissor;

        VkPipelineDynamicStateCreateInfo dynamicState = new()
        {
            dynamicStateCount = 2,
            pDynamicStates = dynamics
        };

        // Use the global descriptor set layout (camera + sampler) when creating the pipeline layout so
        // that descriptor sets are compatible with this pipeline.
        VkDescriptorSetLayout* setLayouts = stackalloc VkDescriptorSetLayout[1];
        setLayouts[0] = _cameraSetLayout;

        VkPipelineLayoutCreateInfo layoutInfo = new()
        {
            setLayoutCount = 1,
            pSetLayouts = setLayouts
        };
        _deviceApi.vkCreatePipelineLayout(_device, &layoutInfo, null, out VkPipelineLayout layout).CheckResult();

        VkGraphicsPipelineCreateInfo pipelineInfo = new()
        {
            stageCount = 2,
            pStages = stages,
            pVertexInputState = &vertexInput,
            pInputAssemblyState = &inputAssembly,
            pViewportState = &viewportState,
            pRasterizationState = &rasterizer,
            pMultisampleState = &multisample,
            pColorBlendState = &colorBlend,
            pDynamicState = &dynamicState,
            layout = layout,
            renderPass = rpHandle,
            subpass = 0
        };

        VkPipeline pipeline;
        _deviceApi.vkCreateGraphicsPipelines(_device, default, 1, &pipelineInfo, null, &pipeline).CheckResult();
        return new VulkanGraphicsPipeline(this, pipeline, layout);
    }

    public void BindGraphicsPipeline(ICommandBuffer commandBuffer, IPipeline pipeline)
    {
        if (commandBuffer is not VulkanCommandBuffer vkCmd)
            throw new ArgumentException("Command buffer was not created by this device.", nameof(commandBuffer));
        if (pipeline is not VulkanGraphicsPipeline vkPipeline)
            throw new ArgumentException("Pipeline was not created by this device.", nameof(pipeline));

        _deviceApi.vkCmdBindPipeline(vkCmd.Handle, VkPipelineBindPoint.Graphics, vkPipeline.Pipeline);
    }

    public void BindDescriptorSet(ICommandBuffer commandBuffer, IPipeline pipeline, IDescriptorSet descriptorSet)
    {
        if (commandBuffer is not VulkanCommandBuffer vkCmd)
            throw new ArgumentException("Command buffer was not created by this device.", nameof(commandBuffer));
        if (pipeline is not VulkanGraphicsPipeline vkPipeline)
            throw new ArgumentException("Pipeline was not created by this device.", nameof(pipeline));
        if (descriptorSet is not VulkanDescriptorSet vkSet)
            throw new ArgumentException("Descriptor set was not created by this device.", nameof(descriptorSet));

        VkDescriptorSet* sets = stackalloc VkDescriptorSet[1];
        sets[0] = vkSet.Handle;

        _deviceApi.vkCmdBindDescriptorSets(vkCmd.Handle, VkPipelineBindPoint.Graphics, vkPipeline.Layout, 0, 1, sets, 0, null);
    }

    public void Draw(ICommandBuffer commandBuffer, uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
    {
        if (commandBuffer is not VulkanCommandBuffer vkCmd)
            throw new ArgumentException("Command buffer was not created by this device.", nameof(commandBuffer));

        _deviceApi.vkCmdDraw(vkCmd.Handle, vertexCount, instanceCount, firstVertex, firstInstance);
    }

    IShader IGraphicsDevice.CreateShader(ShaderDesc desc) => CreateShader(desc);
    IPipeline IGraphicsDevice.CreateGraphicsPipeline(GraphicsPipelineDesc desc) => CreateGraphicsPipeline(desc);
}
