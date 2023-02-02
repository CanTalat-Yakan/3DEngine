﻿using System.IO;
using System;
using Engine.Data;

namespace Engine.Helper
{
    internal class ModelLoader
    {
        public static MeshInfo LoadFilePro(string fileName)
        {
            string assetsPath = Path.Combine(AppContext.BaseDirectory, @"Assets\Engine\Resources\");
            string modelFile = Path.Combine(assetsPath, fileName);

            Assimp.AssimpContext con = new();
            Assimp.Scene file = con.ImportFile(modelFile);

            MeshInfo obj = new();

            obj.Vertices = new();
            obj.Indices = new();
            foreach (var mesh in file.Meshes)
            {
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

                foreach (var face in mesh.Faces)
                {
                    obj.Indices.AddRange(new[] {
                        (ushort)face.Indices[0],
                        (ushort)face.Indices[2],
                        (ushort)face.Indices[1]});

                    if (face.IndexCount == 4)
                        obj.Indices.AddRange(new[] {
                            (ushort)face.Indices[0],
                            (ushort)face.Indices[3],
                            (ushort)face.Indices[2]});
                }
            }

            return obj;
        }
    }
}
