using System.IO;

using static pxr.UsdGeom;
using static pxr.UsdShade;

namespace Engine.Loader;

public sealed class SceneLoader
{
    public static void Save(string localPath, SystemManager systemManager)
    {
        var scene = Scene.Create(localPath);
        var stage = scene.Stage;

        SaveEntities(stage, systemManager.MainEntityManager, $"/World/{systemManager.MainEntityManager.Name}");

        foreach (var subscene in systemManager.SubEntityManagers)
        {
            var subscenePath = Path.Combine(Path.GetDirectoryName(localPath), $"{subscene.Name}.usda");
            SaveEntities(stage, subscene, $"/World/{subscene.Name}");

            var subsceneLayer = SdfLayer.CreateNew(subscenePath);
            stage.GetRootLayer().GetSubLayerPaths().push_back(subsceneLayer.GetDisplayName());
        }

        scene.Save();
    }

    private static void SaveEntities(UsdStage stage, EntityManager EntityManager, string rootPath)
    {
        foreach (var entity in EntityManager.Entities.Values)
        {
            var entityPath = new SdfPath($"{rootPath}/{entity.Data.Name}");
            var usdPrim = stage.DefinePrim(entityPath, new TfToken("Xform"));

            foreach (var component in entity.GetComponentTypes())
                foreach (var attribute in component.GetProperties())
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

    public static void Load(SystemManager systemManager, SceneFiles sceneFile) =>
        Load(systemManager, AssetPaths.SCENENES + sceneFile + ".usdz");

    public static void Load(SystemManager systemManager, string localPath)
    {
        var scene = Scene.Open(localPath);
        var stage = scene.Stage;

        UsdGeomSetStageUpAxis(stage, UsdGeomTokens.y);
        UsdGeomSetStageMetersPerUnit(stage, 1);

        LoadEntities(stage, systemManager.MainEntityManager, $"/World/{systemManager.MainEntityManager.Name}");

        var subLayerPaths = stage.GetRootLayer().GetSubLayerPaths();
        for (uint i = 0; i < subLayerPaths.size(); i++)
        {
            //var subscenePath = subLayerPaths[i];
            //var subscene = new EntityManager { Name = Path.GetFileNameWithoutExtension(subscenePath) };
            //var subsceneLayer = Scene.Open(subscenePath).Stage;
            //LoadEntities(subsceneLayer, subscene, $"/World/{subscene.Name}");

            //systemManager.SubEntityManagers.Add(subscene);
        }
    }

    private static void LoadEntities(UsdStage stage, EntityManager EntityManager, string rootPath)
    {
        foreach (var prim in stage.Traverse())
        {
            var a = prim.GetName();
            var b = prim.GetPath();
            var c = prim.GetPrimPath();
            var d = prim.GetPrimTypeInfo();
            var e = prim.GetType();
            var type = prim.GetTypeName(); // Important
            var g = prim.GetPrimTypeInfo();

            if (type.ToString().Equals("Mesh"))
            {
                var meshData = ModelLoader.ConvertMeshFromUSD(prim);

                EntityData entityData = new() { Name = prim.GetName() };
                Entity entity = EntityManager.CreateEntity(entityData);

                var mesh = entity.AddComponent<Mesh>();
                mesh.SetMeshData(meshData);
                mesh.SetMaterialTextures([new("Default.png", 0)]);
                mesh.SetMaterialPipeline("SimpleLit");
            }

            if (type.ToString().Equals("Xform"))
            {
                EntityData entityData = new() { Name = prim.GetName() };
                Entity entity = EntityManager.CreateEntity(entityData);

                foreach (var usdAttribute in prim.GetAttributes())
                {
                    var componentType = Type.GetType(usdAttribute.GetName());
                    if (componentType is not null)
                    {
                        var component = entity.AddComponent(componentType);
                        component.Entity = entity;

                        var value = usdAttribute.Get();
                        var property = componentType.GetProperty(usdAttribute.GetName());
                        if (property is not null && value is not null)
                            property.SetValue(component, Convert.ChangeType(value, property.PropertyType));
                    }
                }
            }
        }
    }
}


/*
 // Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using USD.NET;

namespace Unity.USD.Core.Examples
{
    public class HelloUsdExample : MonoBehaviour
    {
        [System.Serializable]
        class MyCustomData : SampleBase
        {
            public string aString;
            public int[] anArrayOfInts;
            public Bounds aBoundingBox;
        }

        void Start()
        {
            InitUsd.Initialize();
            Test();
        }

        void Test()
        {
            string usdFile = System.IO.Path.Combine(UnityEngine.Application.dataPath, "sceneFile.usda");

            // Populate Values.
            var value = new MyCustomData();
            value.aString = "Hello from your USD example. Time for some LIVRPS?";
            value.anArrayOfInts = new int[] { 1, 2, 3, 4 };
            value.aBoundingBox = new UnityEngine.Bounds();

            // Writing the value.
            var scene = Scene.Create(usdFile);
            scene.Time = 1.0;
            scene.Write("/someValue", value);
            Debug.Log(scene.Stage.GetRootLayer().ExportToString());
            scene.Save();
            scene.Close();

            // Reading the value.
            Debug.Log(usdFile);
            value = new MyCustomData();
            scene = Scene.Open(usdFile);
            scene.Time = 1.0;
            scene.Read("/someValue", value);

            Debug.LogFormat("Value: string={0}, ints={1}, bbox={2}",
                value.aString, value.anArrayOfInts, value.aBoundingBox);

            var prim = scene.Stage.GetPrimAtPath(new pxr.SdfPath("/someValue"));
            var vtValue = prim.GetAttribute(new pxr.TfToken("aString")).Get(1);
            Debug.Log((string)vtValue);

            scene.Close();
        }
    }
}

 */