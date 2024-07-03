using USD.NET;
using pxr;

namespace Engine.Loader;

public sealed class SceneLoader
{
    public void Save(string localPath, EntityManager mainScene, EntityManager[] subscenes)
    {
        var scene = Scene.Create(localPath);
        var stage = scene.Stage;

        SaveEntities(stage, mainScene, $"/World/{mainScene.Name}");

        foreach (var subscene in subscenes)
        {
            var subscenePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(localPath), $"{subscene.Name}.usda");
            SaveEntities(stage, subscene, $"/World/{subscene.Name}");

            var subsceneLayer = SdfLayer.CreateNew(subscenePath);
            stage.GetRootLayer().GetSubLayerPaths().push_back(subsceneLayer.GetIdentifier());
        }

        scene.Save();
    }

    private void SaveEntities(UsdStage stage, EntityManager scene, string rootPath)
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

    public void Load(out SystemManager systemManager, string localPath)
    {
        systemManager = new SystemManager();

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

    private void LoadEntities(UsdStage stage, EntityManager scene, string rootPath)
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
