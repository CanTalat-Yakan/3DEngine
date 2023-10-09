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
        EventOnDestroy(this, null);

    object ICloneable.Clone() =>
        Clone();

    public Component Clone() =>
        (Component)MemberwiseClone();
}

public partial class Component : ICloneable
{
    public virtual void OnRegister() =>
        ScriptSystem.Register(this);

    public virtual void OnAwake() { }

    public virtual void OnStart() { }

    public virtual void OnUpdate() { }

    public virtual void OnLateUpdate() { }

    public virtual void OnFixedUpdate() { }

    public virtual void OnRender() { }

    public virtual void OnDestroy() { }
}

public interface IHide { }

public class EditorComponent : Component, IHide
{
    public override void OnRegister() =>
        EditorScriptSystem.Register(this);
}
