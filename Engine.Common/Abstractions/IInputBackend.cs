namespace Engine;

/// <summary>
/// Backend contract for platform-specific input event forwarding.
/// Implementations bridge platform input (SDL, WinForms, etc.) into the engine's
/// <see cref="Input"/> resource during <see cref="Initialize"/>.
/// </summary>
public interface IInputBackend
{
    /// <summary>Wires platform-specific input events to the engine's <see cref="Input"/> resource.</summary>
    void Initialize(App app, Input input);
}

