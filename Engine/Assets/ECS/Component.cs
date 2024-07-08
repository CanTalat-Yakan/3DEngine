using EnTTSharp.Entities;

namespace Engine.ECS;

public partial class Component
{
    [Hide] public EntityData EntityData { get; init; }
    [Hide] public EntityKey EntityKey;

    [Hide] public Action EventOnDestroy;

    [Hide] public byte Order = 0;
    [Hide] public bool IsEnabled;

    public Component() { }

    public SystemManager SystemManager => Kernel.Instance.SystemManager;

    public void InvokeEventOnDestroy() =>
        EventOnDestroy();
}

public partial class Component
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

    public virtual void OnGUI() { }
}

public partial class Component : ICloneable, IDisposable
{
    object ICloneable.Clone() =>
        Clone();

    public virtual Component Clone() =>
        (Component)MemberwiseClone();

    public virtual void Dispose() =>
        InvokeEventOnDestroy();
}

public interface IHide { }

public class EditorComponent : Component, IHide
{
    public override void OnRegister() =>
        EditorScriptSystem.Register(this);
}