namespace Engine;

/// <summary>Extracts data from the app world into the render world.</summary>
public interface IExtractSystem { void Run(object appWorld, RenderWorld renderWorld); }

/// <summary>Prepares render resources using extracted data.</summary>
public interface IPrepareSystem
{
    void Run(RenderWorld renderWorld, RendererContext ctx);
}

/// <summary>Records GPU commands from prepared render data.</summary>
public interface IQueueSystem
{
    void Run(RenderWorld renderWorld, RendererContext ctx, CommandRecordingContext cmds);
}

/// <summary>A named node in the render graph with explicit dependencies.</summary>
public interface IRenderNode
{
    string Name { get; }
    IReadOnlyCollection<string> Dependencies { get; }
    void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld);
}