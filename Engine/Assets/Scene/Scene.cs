namespace Engine.Utilities;

public sealed class Scene : ICloneable
{
    public Guid ID = Guid.NewGuid();
    public EntityManager EntityManager = new();

    public string Name = "Scene";
    public bool IsEnabled;

    public void Load() { }

    public void Unload() { }

    object ICloneable.Clone() =>
        Clone();

    public Scene Clone()
    {
        // Copy the current scene object using memberwise clone method.
        var newScene = (Scene)this.MemberwiseClone();

        // Assign a new Guid to the cloned scene object.
        newScene.ID = Guid.NewGuid();

        // Return the cloned scene object.
        return newScene;
    }
}
