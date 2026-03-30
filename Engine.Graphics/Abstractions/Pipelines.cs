namespace Engine;

// Pipeline and render pass abstractions

public interface IFramebuffer { }
public interface IRenderPass { }
public interface IPipeline { }
public interface ICommandBuffer { }

public readonly record struct GraphicsPipelineDesc(IRenderPass RenderPass, IShader VertexShader, IShader FragmentShader);

