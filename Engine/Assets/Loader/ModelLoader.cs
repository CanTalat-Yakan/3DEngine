using System.Collections.Generic;
using System.IO;

using static pxr.UsdGeom;
using static pxr.UsdShade;

namespace Engine.Loader;

public sealed partial class ModelLoader
{
    public static CommonContext Context => _context ??= Kernel.Instance.Context;
    public static CommonContext _context;

    public static MeshData LoadFile(ModelFiles modelFile, InputLayoutHelper inputLayoutElementsHelper = null) =>
        LoadFile(AssetPaths.MODELS + modelFile + ".obj", inputLayoutElementsHelper);

    public static MeshData LoadFile(string filePath, InputLayoutHelper inputLayoutElementsHelper = null)
    {
        var meshName = new FileInfo(filePath).Name;
        if (Assets.Meshes.ContainsKey(meshName))
            return Assets.Meshes[meshName];

       string inputLayoutElements = inputLayoutElementsHelper is not null 
            ? inputLayoutElementsHelper.GetString() 
            : InputLayoutHelper.GetDefault();

        Assimp.AssimpContext context = new();
        //context.SetConfig(new Assimp.Configs.NormalSmoothingAngleConfig(66.0f));

        var postProcessSteps =
              Assimp.PostProcessSteps.Triangulate
            | Assimp.PostProcessSteps.GenerateSmoothNormals
            | Assimp.PostProcessSteps.FlipUVs
            | Assimp.PostProcessSteps.JoinIdenticalVertices
            | Assimp.PostProcessSteps.PreTransformVertices
            | Assimp.PostProcessSteps.CalculateTangentSpace
            | Assimp.PostProcessPreset.TargetRealTimeQuality;

        Assimp.Scene file = context.ImportFile(filePath, postProcessSteps);

        List<int> indices = new();
        List<float> vertices = new();

        List<Vector3> positions = new();

        foreach (var mesh in file.Meshes)
        {
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                if (mesh.HasVertices)
                    positions.Add(new(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z));

                for (int j = 0; j < inputLayoutElements.Length; j++)
                    vertices.AddRange(inputLayoutElements[j] switch
                    {
                        'P' => [mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z],
                        'N' => [mesh.Normals[i].X, mesh.Normals[i].Y, mesh.Normals[i].Z],
                        'T' => [mesh.Tangents[i].X, mesh.Tangents[i].Y, mesh.Tangents[i].Z],
                        'C' => [mesh.VertexColorChannels[0][i].R, mesh.VertexColorChannels[0][i].G, mesh.VertexColorChannels[0][i].B, mesh.VertexColorChannels[0][i].A],

                        'p' => [mesh.Vertices[i].X, mesh.Vertices[i].Y],
                        't' => [mesh.TextureCoordinateChannels[0][i].X, mesh.TextureCoordinateChannels[0][i].Y],
                        'c' => [mesh.VertexColorChannels[0][i].R],
                        _ => throw new NotImplementedException("error input element in model loader"),
                    });
            }

            foreach (var face in mesh.Faces)
                indices.AddRange([face.Indices[0], face.Indices[1], face.Indices[2]]);
        }

        return Context.CreateMeshData(vertices.ToArray(), indices.ToArray(), positions.ToArray(), meshName, inputLayoutElements);
    }
}

public sealed partial class ModelLoader
{
    public static UsdGeomMesh ConvertMeshToUSD(MeshData mesh, UsdGeomMesh usdMesh)
    {
        usdMesh.CreateOrientationAttr(UsdGeomTokens.leftHanded);
        usdMesh.CreatePointsAttr();
        usdMesh.CreateFaceVertexCountsAttr();
        usdMesh.CreateFaceVertexIndicesAttr();
        usdMesh.CreateExtentAttr();

        return usdMesh;
    }

    public static MeshData ConvertMeshFromUSD(UsdPrim prim)
    {
        var meshName = prim.GetName();
        if (Assets.Meshes.ContainsKey(meshName))
            return Assets.Meshes[meshName];

        UsdGeomMesh usdMesh = new(prim);

        List<int> indices = new();
        List<float> vertices = new();

        List<Vector3> positions = new();

        // Read normals
        VtVec3fArray normals = usdMesh.GetNormalsAttr().Get();

        // Read tangents
        //VtVec3fArray tangents = usdMesh.GetTangentsAttr().Get();

        // Read UVs
        //VtVec2fArray uvs = usdMesh.GetPrimvar("st").Get(); // Typically UVs are stored in "st" primvar

        // Read points (vertices)
        VtVec3fArray points = usdMesh.GetPointsAttr().Get();
        for (int i = 0; i < points.size(); i++)
        {
            vertices.AddRange([points[i][0], points[i][1], points[i][2]]);
            vertices.AddRange([normals[i][0], normals[i][1], normals[i][2]]);
            vertices.AddRange([0, 0, 0]);
            vertices.AddRange([0, 0]);

            positions.Add(new(points[i][0], points[i][1], points[i][2]));
        }

        // Read face vertex counts
        VtIntArray faceVertexCounts = usdMesh.GetFaceVertexCountsAttr().Get();

        // Read face vertex indices
        VtIntArray faceVertexIndices = usdMesh.GetFaceVertexIndicesAttr().Get();

        // Triangulate faces consisting of 4 or more vertices per face
        UsdGeomMesh.Triangulate(faceVertexIndices, faceVertexCounts);

        int idx = 0;
        for (int i = 0; i < faceVertexCounts.size(); i++)
            for (int j = 0; j < faceVertexCounts[i]; ++j)
                indices.Add(faceVertexIndices[idx++]);

        return Context.CreateMeshData(vertices.ToArray(), indices.ToArray(), positions.ToArray(), meshName, "PNTt");
    }

    public static UsdShadeMaterial ConvertMaterialToUSD(Components.Material material, UsdShadeMaterial usdMaterial)
    {
        return usdMaterial;
    }

    public static UsdShadeMaterial ConvertMaterialFromUSD(UsdPrim prim)
    {
        UsdShadeMaterial material = new(prim);

        return material;
    }
}