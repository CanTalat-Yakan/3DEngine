﻿using System.Collections.Generic;
using System.IO;

using Assimp;

namespace Engine.Loader;

public sealed class ModelLoader
{
    private static Dictionary<string, MeshInfo> s_meshInfoStore = new();

    public static MeshInfo LoadFile(string filePath, bool fromResources = true)
    {
        if (s_meshInfoStore.ContainsKey(filePath))
            return s_meshInfoStore[filePath];

        // Create an AssimpContext instance.
        AssimpContext context = new();
        //context.SetConfig(new NormalSmoothingAngleConfig(66.0f));

        var postProcessSteps =
            PostProcessSteps.Triangulate |
            PostProcessSteps.GenerateSmoothNormals |
            PostProcessSteps.FlipUVs |
            PostProcessSteps.JoinIdenticalVertices |
            PostProcessSteps.PreTransformVertices |
            PostProcessSteps.CalculateTangentSpace |
            PostProcessPreset.TargetRealTimeQuality;

        string modelFilePath = fromResources
            ? Paths.MODELS + filePath
            : filePath;

        // Load the model file using Assimp.
        Assimp.Scene file = context.ImportFile(modelFilePath, postProcessSteps);

        // Create new lists for the "MeshInfo" object.
        var vertices = new List<Vertex>();
        var indices = new List<int>();

        // Iterate over all the meshes in the file.
        foreach (var mesh in file.Meshes)
        {
            // Add each vertex to the list.
            for (int i = 0; i < mesh.VertexCount; i++)
                vertices.Add(new()
                {
                    Position = mesh.HasVertices ? new Vector3(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z) : Vector3.Zero,
                    TextureCoordinate = mesh.HasTextureCoords(0) ? new Vector2(mesh.TextureCoordinateChannels[0][i].X, mesh.TextureCoordinateChannels[0][i].Y) : Vector2.Zero,
                    Normal = mesh.HasNormals ? new Vector3(mesh.Normals[i].X, mesh.Normals[i].Y, mesh.Normals[i].Z) : Vector3.Zero,
                    Tangent = mesh.HasTangentBasis ? new Vector3(mesh.Tangents[i].X, mesh.Tangents[i].Y, mesh.Tangents[i].Z) : Vector3.Zero,
                });

            // Add each face to the list.
            foreach (var face in mesh.Faces)
            {
                indices.AddRange(new[] {
                        face.Indices[0],
                        face.Indices[1],
                        face.Indices[2]});

                // Split the face into two triangles,
                // when the face has four indices. 
                if (face.IndexCount == 4)
                    indices.AddRange(new[] {
                        face.Indices[0],
                        face.Indices[2],
                        face.Indices[3]});
            }
        }

        MeshInfo meshInfo = new()
        {
            Vertices = vertices.ToArray(),
            Indices = indices.ToArray()
        };

        // Add the MeshInfo into the store with the filePath as key.
        s_meshInfoStore.Add(filePath, meshInfo);

        // Return the completed MeshInfo object.
        return meshInfo;
    }
}
