namespace Engine.ECS;

public partial class Component : ICloneable
{
    [Hide] public Entity Entity;
    [Hide] public byte Order = 0;
    [Hide] public bool IsEnabled;

    [Hide] public event EventHandler EventOnDestroy;

    public Component() =>
        OnRegister();

    public void InvokeEventOnDestroy() =>
        // Invoke the Event when the Component is destroyed.
        EventOnDestroy(this, null);

    object ICloneable.Clone() =>
        Clone();

    public Component Clone() =>
        (Component)MemberwiseClone();
}

public partial class Component : ICloneable
{
    public virtual void OnRegister() { }

    public virtual void OnAwake() { }

    public virtual void OnStart() { }

    public virtual void OnUpdate() { }

    public virtual void OnLateUpdate() { }

    public virtual void OnFixedUpdate() { }

    public virtual void OnRender() { }

    public virtual void OnDestroy() { }
}

public class EditorComponent : Component, IHide { }

public interface IHide { }
