namespace Engine;

/// <summary>
/// Abstraction for driving the main loop.
/// Implemented by window backends (e.g., SDL) or editors that provide their own frame cadence.
/// </summary>
public interface IMainLoopDriver
{
    /// <summary>
    /// Runs the application loop, invoking <paramref name="frameStep"/> once per frame
    /// until the loop ends (e.g., window close, editor stop).
    /// </summary>
    void Run(Action frameStep);
}

