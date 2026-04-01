namespace Editor.Shell;

/// <summary>
/// Central registry holding the current <see cref="ShellDescriptor"/> and
/// <see cref="InspectableComponentDescriptor"/> list. Observable — fires
/// <see cref="Changed"/> whenever the descriptor tree is rebuilt (e.g., after
/// a script hot-reload). Thread-safe for concurrent reads and atomic swaps.
/// </summary>
public sealed class ShellRegistry
{
    private readonly Lock _lock = new();
    private ShellDescriptor _current = new();
    private List<InspectableComponentDescriptor> _components = [];
    private int _version;

    /// <summary>Fired when the shell descriptor is replaced (hot-reload).</summary>
    public event Action? Changed;

    /// <summary>Monotonically increasing version; bumped on every swap.</summary>
    public int Version { get { lock (_lock) return _version; } }

    /// <summary>Current shell descriptor snapshot.</summary>
    public ShellDescriptor Current
    {
        get { lock (_lock) return _current; }
    }

    /// <summary>Current inspectable component descriptors.</summary>
    public IReadOnlyList<InspectableComponentDescriptor> Components
    {
        get { lock (_lock) return _components; }
    }

    /// <summary>
    /// Atomically replaces the shell descriptor and fires <see cref="Changed"/>.
    /// Called by the script compiler after a successful rebuild.
    /// </summary>
    public void Update(ShellDescriptor descriptor)
    {
        lock (_lock)
        {
            _current = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            _version++;
        }
        Changed?.Invoke();
    }

    /// <summary>Updates only the inspectable components (e.g., after behavior scripts are reloaded).</summary>
    public void UpdateComponents(List<InspectableComponentDescriptor> components)
    {
        lock (_lock)
        {
            _components = components ?? [];
            _version++;
        }
        Changed?.Invoke();
    }

    /// <summary>Atomically replaces both the shell descriptor and component descriptors.</summary>
    public void Update(ShellDescriptor descriptor, List<InspectableComponentDescriptor> components)
    {
        lock (_lock)
        {
            _current = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            _components = components ?? [];
            _version++;
        }
        Changed?.Invoke();
    }
}
