using Assimp.Configs;
using Assimp;
using System.Collections.Generic;
using System.IO;

namespace Engine.Helper;

internal sealed class ModelLoader
{
    public static MeshInfo LoadFile(string filePath, bool fromResources = true)
    {
        string modelFilePath = filePath;
        if (fromResources)
        {
            // Combine the base directory and the relative path to the resources directory
            string resourcesPath = Path.Combine(AppContext.BaseDirectory, Paths.MODELS);
            // Define the full path to the model file.
            modelFilePath = Path.Combine(resourcesPath, filePath);
        }

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

        // Load the model file using Assimp.
        Assimp.Scene file = context.ImportFile(modelFilePath, postProcessSteps);

        // Create new lists for the "MeshInfo" object.
        var vertices = new List<Vertex>();
        var indices = new List<ushort>();

        // Iterate over all the meshes in the file.
        foreach (var mesh in file.Meshes)
        {
            // Add each vertex to the list.
            for (int i = 0; i < mesh.VertexCount; i++)
                vertices.Add(new()
                {
                    Position = mesh.HasVertices ? new Vector3(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z) : Vector3.Zero,
                    TexCoord = mesh.HasTextureCoords(0) ? new Vector2(mesh.TextureCoordinateChannels[0][i].X, mesh.TextureCoordinateChannels[0][i].Y) : Vector2.Zero,
                    Normal = mesh.HasNormals ? new Vector3(mesh.Normals[i].X, mesh.Normals[i].Y, mesh.Normals[i].Z) : Vector3.Zero,
                    Tangent = mesh.HasTangentBasis ? new Vector3(mesh.Tangents[i].X, mesh.Tangents[i].Y, mesh.Tangents[i].Z) : Vector3.Zero,
                });

            // Add each face to the list.
            foreach (var face in mesh.Faces)
            {
                indices.AddRange(new[] {
                        (ushort)face.Indices[0],
                        (ushort)face.Indices[1],
                        (ushort)face.Indices[2]});

                // Split the face into two triangles,
                // when the face has four indices. 
                if (face.IndexCount == 4)
                    indices.AddRange(new[] {
                        (ushort)face.Indices[0],
                        (ushort)face.Indices[2],
                        (ushort)face.Indices[3]});
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
