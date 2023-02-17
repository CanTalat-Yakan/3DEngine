﻿using Assimp.Configs;
using Assimp;
using System.Collections.Generic;
using System.IO;

namespace Engine.Helper;

internal class ModelLoader
{
    public static MeshInfo LoadFile(string filePath, bool fromResources = true)
    {
        string modelFilePath = filePath;
        if (fromResources)
        {
            // Combine the base directory and the relative path to the resources directory
            string resourcesPath = Path.Combine(AppContext.BaseDirectory, @"Assets\Engine\Resources");
            // Define the full path to the model file.
            modelFilePath = Path.Combine(resourcesPath, filePath);
        }

        // Create an AssimpContext instance.
        AssimpContext con = new();
        AssimpContext importer = new();
        importer.SetConfig(new NormalSmoothingAngleConfig(66.0f));

        // Load the model file using Assimp.
        Assimp.Scene file = con.ImportFile(modelFilePath, PostProcessPreset.TargetRealTimeFast);

        // Create new lists for the "MeshInfo" object.
        var vertices = new List<Vertex>();
        var indices = new List<ushort>();

        // Iterate over all the meshes in the file.
        foreach (var mesh in file.Meshes)
        {
            // Add each vertex to the list.
            for (int i = 0; i < mesh.VertexCount; i++)
                vertices.Add(new(
                    mesh.Vertices[i].X,
                    mesh.Vertices[i].Y,
                    mesh.Vertices[i].Z,
                    mesh.TextureCoordinateChannels[0][i].X,
                    1 - mesh.TextureCoordinateChannels[0][i].Y,
                    mesh.Normals[i].X,
                    mesh.Normals[i].Y,
                    mesh.Normals[i].Z));

            // Add each face to the list.
            foreach (var face in mesh.Faces)
            {
                indices.AddRange(new[] {
                        (ushort)face.Indices[0],
                        (ushort)face.Indices[2],
                        (ushort)face.Indices[1]});

                // If the face has four vertices, add one additional triangles to the MeshInfo object.
                if (face.IndexCount == 4)
                    indices.AddRange(new[] {
                            (ushort)face.Indices[0],
                            (ushort)face.Indices[3],
                            (ushort)face.Indices[2]});
            }
        }

        // Return the completed "MeshInfo" object.
        return new MeshInfo()
        {
            Vertices = vertices.ToArray(),
            Indices = indices.ToArray()
        };
    }
}
