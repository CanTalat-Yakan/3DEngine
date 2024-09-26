namespace Engine.ECS;

[Flags]
public enum ComponentMethods
{
    None = 0,
    Awake = 1,
    Start = 2,
    Update = 4,
    LateUpdate = 8,
    FixedUpdate = 16,
    Render = 32,
    Destroy = 64,
    GUI = 128,
    All = Awake | Start | Update | LateUpdate | FixedUpdate | Render | Destroy | GUI
}

public interface IComponent { }

public partial class Component : IComponent
{
    [Hide] public Entity Entity;

    [Hide] public byte Order = 0;
    [Hide] public bool IsEnabled;

    [Hide] public Action EventOnDestroy;

    [Hide] public ComponentMethods BitFlag = ComponentMethods.All;

    public Component() { }

    public void InvokeEventOnDestroy() =>
        EventOnDestroy();

    public void Destroy() =>
        Entity.Manager.DestroyEntity(Entity);
}

public partial class Component
{
    public virtual void OnRegister() =>
        ScriptSystem.Register(this);

    public virtual void OnAwake() =>
        BitFlag &= ~ComponentMethods.Awake;

    public virtual void OnStart() =>
        BitFlag &= ~ComponentMethods.Start;

    public virtual void OnUpdate() =>
        BitFlag &= ~ComponentMethods.Update;

    public virtual void OnLateUpdate() =>
        BitFlag &= ~ComponentMethods.LateUpdate;

    public virtual void OnFixedUpdate() =>
        BitFlag &= ~ComponentMethods.FixedUpdate;

    public virtual void OnRender() =>
        BitFlag &= ~ComponentMethods.Render;

    public virtual void OnDestroy() =>
        BitFlag &= ~ComponentMethods.Destroy;

    public virtual void OnGUI() =>
        BitFlag &= ~ComponentMethods.GUI;
}

public partial class Component : ICloneable, IDisposable
{
    object ICloneable.Clone() =>
        Clone();

    public Component Clone() =>
        (Component)MemberwiseClone();

    public void Dispose() =>
        InvokeEventOnDestroy();
}

public interface IHide { }

public class EditorComponent : Component, IHide
{
    public override void OnRegister() =>
        EditorScriptSystem.Register(this);
}

public partial class SimpleComponent : Component, IHide
{
    public override void OnRegister() =>
        SimpleSystem.Register(this);

    public override void OnUpdate() { }
}
