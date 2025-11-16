namespace Engine;

public interface IExtractSystem { void Run(object appWorld, RenderWorld renderWorld); }

public interface IPrepareSystem
{
    void Run(RenderWorld renderWorld, RendererContext ctx);
}

public interface IQueueSystem
{
    void Run(RenderWorld renderWorld, RendererContext ctx, CommandRecordingContext cmds);
}

public interface IRenderNode
{
    string Name { get; }
    IReadOnlyCollection<string> Dependencies { get; }
    void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld);
}