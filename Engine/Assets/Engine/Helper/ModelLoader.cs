using System.IO;
using System;
using Engine.Data;

namespace Engine.Helper
{
    internal class ModelLoader
    {
        public static MeshInfo LoadFile(string filePath)
        {
            // Combine the base directory and the relative path to the resources directory
            string resourcesPath = Path.Combine(AppContext.BaseDirectory, @"Assets\Engine\Resources");
            // Define the full path to the model file.
            string modelFilePath = Path.Combine(resourcesPath, filePath);

            // Create an AssimpContext instance.
            Assimp.AssimpContext con = new();
            // Load the model file using Assimp.
            Assimp.Scene file = con.ImportFile(modelFilePath);

            // Create a new MeshInfo object.
            MeshInfo obj = new() { Vertices = new(), Indices = new() };
            // Iterate over all the meshes in the file.
            foreach (var mesh in file.Meshes)
            {
                // Add each vertex to the MeshInfo object.
                for (int i = 0; i < mesh.VertexCount; i++)
                    obj.Vertices.Add(new(
                        mesh.Vertices[i].X,
                        mesh.Vertices[i].Y,
                        mesh.Vertices[i].Z,
                        mesh.TextureCoordinateChannels[0][i].X,
                        1 - mesh.TextureCoordinateChannels[0][i].Y,
                        mesh.Normals[i].X,
                        mesh.Normals[i].Y,
                        mesh.Normals[i].Z));

                // Add each face to the MeshInfo object.
                foreach (var face in mesh.Faces)
                {
                    obj.Indices.AddRange(new[] {
                        (ushort)face.Indices[0],
                        (ushort)face.Indices[2],
                        (ushort)face.Indices[1]});

                    // If the face has four vertices, add one additional triangles to the MeshInfo object.
                    if (face.IndexCount == 4)
                        obj.Indices.AddRange(new[] {
                            (ushort)face.Indices[0],
                            (ushort)face.Indices[3],
                            (ushort)face.Indices[2]});
                }
            }

            // Return the completed MeshInfo object.
            return obj;
        }
    }
}
