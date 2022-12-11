using System.Collections.Generic;
using System.IO;
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

            //// initialize Scene class object
            //Scene scene = new Scene();
            //// initialize an object
            //XLoadOptions loadXOpts = new XLoadOptions(FileContentType.ASCII);
            //// flip the coordinate system.
            //loadXOpts.FlipCoordinateSystem = true;
            //// load 3D X file
            //scene.Open(modelFile, loadXOpts);

            //// For complete examples and data files, please go to https://github.com/aspose-3d/Aspose.3D-for-.NET
            //// Initialize an object
            //ObjLoadOptions loadObjOpts = new ObjLoadOptions();
            //// Import materials from external material library file
            //loadObjOpts.EnableMaterials = true;
            //// Flip the coordinate system.
            //loadObjOpts.FlipCoordinateSystem = true;
            //// Configure the look up paths to allow importer to find external dependencies.
            //loadObjOpts.LookupPaths = new List<string>(new string[] { modelFile });

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
                    1-mesh.TextureCoordinateChannels[0][i].Y,
                    mesh.Normals[i].X,
                    mesh.Normals[i].Y,
                    mesh.Normals[i].Z));

                foreach (var face in mesh.Faces)
                {
                    ushort[] rangeIndices = new[] {
                        (ushort)(face.Indices[0]),
                        (ushort)(face.Indices[2]),
                        (ushort)(face.Indices[1])};
                    obj.Indices.AddRange(rangeIndices);
                    if (face.IndexCount == 4)
                    {
                        rangeIndices = new[] {
                        (ushort)(face.Indices[0]),
                        (ushort)(face.Indices[3]),
                        (ushort)(face.Indices[2])};
                        obj.Indices.AddRange(rangeIndices);
                    }
                }
            }

            return obj;
        }
    }
}
