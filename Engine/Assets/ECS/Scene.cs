namespace Engine.ECS;

public sealed partial class Scene
{
    public Guid ID = Guid.NewGuid();
    public EntityManager EntityManager = new();

    public string LocalPath => $"{localPath}\\{Name}.usda";
    public string localPath = "";
    public string Name = "Scene";

    public bool IsEnabled;
}

public sealed partial class Scene : ICloneable
{
    object ICloneable.Clone() =>
        Clone();

    public Scene Clone()
    {
        // Copy the current scene object using memberwise clone method.
        var newScene = (Scene)this.MemberwiseClone();

        // Assign a new Guid to the cloned scene object.
        newScene.ID = Guid.NewGuid();

        return newScene;
    }
}