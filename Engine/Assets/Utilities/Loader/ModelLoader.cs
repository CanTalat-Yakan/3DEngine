using System.Collections.Generic;
using System.IO;

using Assimp;

namespace Engine.Loader;

public sealed class ModelLoader
{
    public static CommonContext Context => _context ??= Kernel.Instance.Context;
    public static CommonContext _context;

    public static MeshInfo LoadFile(string filePath, string inputLayoutElements)
    {
        var meshName = new FileInfo(filePath).Name;
        if (Context.Meshes.ContainsKey(meshName))
            return Context.Meshes[filePath];

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

        Assimp.Scene file = context.ImportFile(filePath, postProcessSteps);

        List<float> vertices = new();
        List<int> indices = new();

        foreach (var mesh in file.Meshes)
        {
            for (int i = 0; i < mesh.VertexCount; i++)
                for (int j = 0; j < inputLayoutElements.Length; j++)
                    vertices.AddRange(inputLayoutElements[j] switch
                    {
                        'P' => [mesh.Vertices[j].X, mesh.Vertices[j].Y, mesh.Vertices[j].Z],
                        'N' => [mesh.Normals[j].X, mesh.Normals[j].Y, mesh.Normals[j].Z],
                        'T' => [mesh.Tangents[j].X, mesh.Tangents[j].Y, mesh.Tangents[j].Z],

                        'C' => [mesh.VertexColorChannels[0][j].R, mesh.VertexColorChannels[0][j].G, mesh.VertexColorChannels[0][j].B],

                        't' => [mesh.TextureCoordinateChannels[0][j].X, mesh.TextureCoordinateChannels[0][j].Y],
                        _ => throw new NotImplementedException("error input element in model loader"),
                    });

            foreach (var face in mesh.Faces)
                indices.AddRange(new[] {
                        face.Indices[0],
                        face.Indices[1],
                        face.Indices[2]});
        }

        var meshInfo = Context.CreateMesh(meshName, Context.CreateInputLayoutDescription(inputLayoutElements));

        GPUUpload upload = new()
        {
            MeshInfo = meshInfo,
            VertexData = vertices.ToArray(),
            IndexData = indices.ToArray(),
        };
        Context.UploadQueue.Enqueue(upload);

        return meshInfo;
    }
}