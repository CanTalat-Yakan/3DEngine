namespace Engine;

// Pipeline and render pass abstractions

public interface IFramebuffer { }
public interface IRenderPass { }
public interface IPipeline { }
public interface ICommandBuffer { }

// Vertex input descriptions for custom vertex layouts
public enum VertexFormat
{
    Float2,   // VK_FORMAT_R32G32_SFLOAT
    Float3,   // VK_FORMAT_R32G32B32_SFLOAT
    Float4,   // VK_FORMAT_R32G32B32A32_SFLOAT
    UNormR8G8B8A8 // VK_FORMAT_R8G8B8A8_UNORM
}

public enum IndexType { UInt16, UInt32 }

[Flags]
public enum ShaderStageFlags
{
    Vertex = 1,
    Fragment = 2,
    All = Vertex | Fragment
}

public readonly record struct VertexInputBindingDesc(uint Binding, uint Stride);
public readonly record struct VertexInputAttributeDesc(uint Location, uint Binding, VertexFormat Format, uint Offset);
public readonly record struct PushConstantRange(ShaderStageFlags StageFlags, uint Offset, uint Size);

public readonly record struct GraphicsPipelineDesc(
    IRenderPass RenderPass,
    IShader VertexShader,
    IShader FragmentShader,
    bool BlendEnabled = false,
    bool CullBackFace = true,
    VertexInputBindingDesc[]? VertexBindings = null,
    VertexInputAttributeDesc[]? VertexAttributes = null,
    PushConstantRange[]? PushConstantRanges = null);

