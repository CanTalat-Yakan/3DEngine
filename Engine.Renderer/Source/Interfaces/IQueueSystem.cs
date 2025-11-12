namespace Engine;

public interface IQueueSystem { void Run(RenderWorld renderWorld, RendererContext ctx, CommandRecordingContext cmds); }