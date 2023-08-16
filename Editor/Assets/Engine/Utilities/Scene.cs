namespace Engine.Utilities;

public class Scene : BindableBase, ICloneable
{
    public Guid ID = Guid.NewGuid();

    public EntityManager EntityManager = new();

    private string _name = "Scene";
    public string Name
    {
        get { return _name; }
        set { SetProperty(ref _name, value); }
    }

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get { return _isEnabled; }
        set { SetProperty(ref _isEnabled, value); }
    }

    object ICloneable.Clone() =>
        Clone();

    public Scene Clone()
    {
        // Copy the current scene object using MemberwiseClone method.
        var newScene = (Scene)this.MemberwiseClone();

        // Assign a new Guid to the cloned scene object.
        newScene.ID = Guid.NewGuid();

        // Return the cloned scene object.
        return newScene;
    }

    public void Load() { }

    public void Unload() { }
}
