using USD.NET;
using pxr;
using System.IO;

namespace Engine.Loader;

public sealed class SceneLoader
{
    public static void Save(string localPath, SystemManager systemManager)
    {
        //var scene = Scene.Create(localPath);
        //var stage = scene.Stage;
        var stage = UsdStage.CreateNew(localPath);

        SaveEntities(stage, systemManager.MainScene, $"/World/{systemManager.MainScene.Name}");

        foreach (var subscene in systemManager.SubScenes)
        {
            var subscenePath = Path.Combine(Path.GetDirectoryName(localPath), $"{subscene.Name}.usda");
            SaveEntities(stage, subscene, $"/World/{subscene.Name}");

            var subsceneLayer = SdfLayer.CreateNew(subscenePath);
            stage.GetRootLayer().GetSubLayerPaths().push_back(subsceneLayer.GetIdentifier());
        }

        stage.Save();
        //scene.Save();
    }

    private static void SaveEntities(UsdStage stage, EntityManager scene, string rootPath)
    {
        foreach (var entity in scene.List)
        {
            var entityPath = new SdfPath($"{rootPath}/{entity.Name}");
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
    }

    public static void Load(out SystemManager systemManager, string localPath)
    {
        systemManager = new SystemManager();

        if (!File.Exists(localPath))
            throw new FileNotFoundException("USD file not found at path: " + localPath);
        var scene = Scene.Open(localPath);
        var stage = scene.Stage;

        LoadEntities(stage, systemManager.MainScene, $"/World/{systemManager.MainScene.Name}");

        var subLayerPaths = stage.GetRootLayer().GetSubLayerPaths();
        for (uint i = 0; i < subLayerPaths.size(); i++)
        {
            //var subscenePath = subLayerPaths[i];
            //var subscene = new EntityManager { Name = System.IO.Path.GetFileNameWithoutExtension(subscenePath) };
            //var subsceneLayer = Scene.Open(subscenePath).Stage;
            //LoadEntities(subsceneLayer, subscene, $"/World/{subscene.Name}");

            //systemManager.Subscenes.Add(subscene);
        }
    }

    private static void LoadEntities(UsdStage stage, EntityManager scene, string rootPath)
    {
        foreach (var prim in stage.Traverse())
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
                            property.SetValue(component, Convert.ChangeType(value, property.PropertyType));

                        entity.Components.Add(component);
                    }
                }

                scene.List.Add(entity);
            }
    }
}
