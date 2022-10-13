using Aspose.ThreeD.Formats;
using Aspose.ThreeD;
using System;
using System.Collections.Generic;
using System.IO;
using WinUI3DEngine.Assets.Engine.Data;
using Assimp.Configs;

namespace WinUI3DEngine.Assets.Engine.Helper
{
    internal class CObjLoader
    {
        internal static CMeshInfo LoadFilePro(string _fileName)
        {
            string assetsPath = Path.Combine(AppContext.BaseDirectory, @"Assets\Engine\Resources\");
            string modelFile = Path.Combine(assetsPath, _fileName);
            
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

            var con = new Assimp.AssimpContext();
            var file = con.ImportFile(modelFile);

            CMeshInfo obj = new CMeshInfo();

            obj.Vertices = new List<CVertex>();
            obj.Indices = new List<ushort>();
            foreach (var mesh in file.Meshes)
            {
                for (int i = 0; i < mesh.VertexCount; i++)
                    obj.Vertices.Add(new CVertex(
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
                    var rangeIndices = new ushort[] {
                        (ushort)(face.Indices[0]),
                        (ushort)(face.Indices[2]),
                        (ushort)(face.Indices[1])};
                    obj.Indices.AddRange(rangeIndices);
                    if (face.IndexCount == 4)
                    {
                        rangeIndices = new ushort[] {
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
