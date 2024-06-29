using USD.NET;
using pxr;

namespace Engine.SceneSystem;

public sealed partial class Scene
{
    public Guid ID = Guid.NewGuid();
    public EntityManager EntityManager = new();

    public string LocalPath => $"{localPath}\\{Name}.usda";
    public string localPath = "";
    public string Name = "Scene";

    public bool IsEnabled;

    public void Save()
    {
        var stage = CreateStage(LocalPath);

        foreach (var entity in EntityManager.EntityList)
        {
            var entityPath = new SdfPath($"/World/{entity.Name}");
            var usdPrim = stage.DefinePrim(entityPath, new TfToken("Xform"));

            foreach (var component in entity.Components)
                foreach (var attribute in component.GetType().GetProperties())
                {
                    var value = attribute.GetValue(component);
                    if (value is not null)
                    {
                        var usdAttribute = usdPrim.CreateAttribute(new TfToken(attribute.Name), SdfValueTypeNames.Token);
                        usdAttribute.Set(new TfToken(value.ToString()));
                    }
                }
        }

        stage.GetRootLayer().Save();
    }
    //{

    //    // Save the scene to a USD file
    //    var path = "scene.usda";
    //    var sceneStage = CreateStage(path);

    //    // Here you would add entities to the USD stage
    //    sceneStage.Save();
    //}

    public void Load()
    {
        var stage = OpenStage(LocalPath);

        foreach (var prim in stage.Traverse())
        {
            if (prim.IsA(TfType.FindByName("Xform")))
            {
                var entity = new Entity { Name = prim.GetName() };

                foreach (var usdAttribute in prim.GetAttributes())
                {
                    var componentType = Type.GetType(usdAttribute.GetName());
                    if (componentType != null)
                    {
                        var component = (Component)Activator.CreateInstance(componentType);
                        component.Entity = entity;

                        var value = usdAttribute.Get();
                        var property = componentType.GetProperty(usdAttribute.GetName());
                        if (property != null && value != null)
                        {
                            property.SetValue(component, Convert.ChangeType(value, property.PropertyType));
                        }

                        entity.Components.Add(component);
                    }
                }

                EntityManager.EntityList.Add(entity);
            }
        }
    }
    //{

    //    // Load the scene from a USD file
    //    var path = "scene.usda";
    //    var sceneStage = OpenStage(path);
    //    // Here you would load entities from the USD stage
    //}

    public void Unload() { }
}

public sealed partial class Scene
{
    public static UsdStage CreateStage(string path) =>
        UsdStage.CreateNew(path);
    //{
    //// Create a new USD stage at the given path
    //var stage = UsdStage.CreateNew(path);

    //// Get the weak pointer to the stage
    //var stageWeakPtr = new UsdStageWeakPtr(stage);

    //// Add a simple cube to the stage
    //var cubePath = new SdfPath("/World/Cube");
    //var cube = UsdGeomCube.Define(stageWeakPtr, cubePath);
    //cube.GetSizeAttr().Set(1.0f);

    //return stage;
    //}

    public static UsdStage OpenStage(string path) =>
        UsdStage.Open(path);
    //// Open an existing USD stage from the given path
    //UsdStage.Open(path);
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