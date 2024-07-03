using Engine.ECS;
using pxr;

namespace Engine.Loader;

public sealed class SceneLoader
{

    public void Save(string localPath, Scene mainScene, Scene[] subscenes)
    {
        var stage = CreateStage(localPath);

        foreach (var entity in mainScene.EntityManager.EntityList)
        {
            var entityPath = new SdfPath($"/World/{mainScene.Name}/{entity.Name}");
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

    public void Load(string localPath, Scene mainScene, Scene[] subScenes)
    {
        var stage = OpenStage(localPath);

        foreach (var prim in stage.Traverse())
        {
            if (prim.IsA(TfType.FindByName("Xform")))
            {
                var entity = new Entity { Name = prim.GetName() };

                foreach (var usdAttribute in prim.GetAttributes())
                {
                    var componentType = Type.GetType(usdAttribute.GetName());
                    if (componentType is not null)
                    {
                        var component = (Component)Activator.CreateInstance(componentType);
                        component.Entity = entity;

                        var value = usdAttribute.Get();
                        var property = componentType.GetProperty(usdAttribute.GetName());
                        if (property is not null && value is not null)
                        {
                            property.SetValue(component, Convert.ChangeType(value, property.PropertyType));
                        }

                        entity.Components.Add(component);
                    }
                }

                mainScene.EntityManager.EntityList.Add(entity);
            }
        }
    }
    //{

    //    // Load the scene from a USD file
    //    var path = "scene.usda";
    //    var sceneStage = OpenStage(path);
    //    // Here you would load entities from the USD stage
    //}

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