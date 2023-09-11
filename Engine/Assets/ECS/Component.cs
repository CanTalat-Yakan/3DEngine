namespace Engine.ECS;

public class Component : ICloneable // BindableBase
{
    [Hide] public Entity Entity;
    [Hide] public byte Order = 0;

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value; // SetProperty(ref _isEnabled, value);
    }

    public event EventHandler _eventOnDestroy;

    public Component() =>
        OnRegister();

    public virtual void OnRegister() { }

    public virtual void OnAwake() { }

    public virtual void OnStart() { }

    public virtual void OnUpdate() { }

    public virtual void OnLateUpdate() { }

    public virtual void OnRender() { }

    public virtual void OnDestroy() { }

    public void InvokeEventOnDestroy() => 
        // Invoke the Event when the Component is destroyed.
        _eventOnDestroy(this, null);

    object ICloneable.Clone() =>
        Clone();

    public Component Clone() =>
        (Component)MemberwiseClone();
}

public class EditorComponent : Component, IHide { }

public interface IHide { }
