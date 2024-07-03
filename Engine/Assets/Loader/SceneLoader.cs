using USD.NET;
//using pxr;

namespace Engine.Loader;

public sealed class SceneLoader
{
    public void Save(string localPath, EntityManager mainScene, EntityManager[] subscenes)
    {
        var stage = Scene.Create();

        //var stage = CreateStage(localPath);

        //foreach (var entity in mainScene.EntityList)
        //{
        //    var entityPath = new SdfPath($"/World/{mainScene.Name}/{entity.Name}");
        //    var usdPrim = stage.DefinePrim(entityPath, new TfToken("Xform"));

        //    foreach (var component in entity.Components)
        //        foreach (var attribute in component.GetType().GetProperties())
        //        {
        //            var value = attribute.GetValue(component);
        //            if (value is not null)
        //            {
        //                var usdAttribute = usdPrim.CreateAttribute(new TfToken(attribute.Name), SdfValueTypeNames.Token);
        //                usdAttribute.Set(new TfToken(value.ToString()));
        //            }
        //        }
        //}

        //stage.GetRootLayer().Save();
    }
    
    public void Load(string localPath, Scene mainScene, Scene[] subScenes)
    {
        //var stage = OpenStage(localPath);

        //foreach (var prim in stage.Traverse())
        //{
        //    if (prim.IsA(TfType.FindByName("Xform")))
        //    {
        //        var entity = new Entity { Name = prim.GetName() };

        //        foreach (var usdAttribute in prim.GetAttributes())
        //        {
        //            var componentType = Type.GetType(usdAttribute.GetName());
        //            if (componentType is not null)
        //            {
        //                var component = (Component)Activator.CreateInstance(componentType);
        //                component.Entity = entity;

        //                var value = usdAttribute.Get();
        //                var property = componentType.GetProperty(usdAttribute.GetName());
        //                if (property is not null && value is not null)
        //                {
        //                    property.SetValue(component, Convert.ChangeType(value, property.PropertyType));
        //                }

        //                entity.Components.Add(component);
        //            }
        //        }

        //        mainScene.EntityList.Add(entity);
        //    }
        //}
    }
}