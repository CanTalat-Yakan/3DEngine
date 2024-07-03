using System.Collections.Generic;
using System.IO;

using Assimp;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Loader;

public sealed class ModelLoader
{
    public static CommonContext Context => _context ??= Kernel.Instance.Context;
    public static CommonContext _context;

    public static MeshInfo LoadFile(string filePath, string inputLayoutElements = "PNTt")
    {
        var meshName = new FileInfo(filePath).Name;
        if (Assets.Meshes.ContainsKey(meshName))
            return Assets.Meshes[meshName];

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

        List<int> indices = new();
        List<float> vertices = new();

        List<Vector3> positions = new();

        foreach (var mesh in file.Meshes)
        {
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                if (mesh.HasVertices)
                    positions.Add(new (mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z));

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

        var meshInfo = Context.CreateMesh(meshName, inputLayoutElements);
        meshInfo.IndexCount = indices.Count;
        meshInfo.VertexCount = positions.Count;
        meshInfo.BoundingBox = BoundingBox.CreateFromPoints(positions.ToArray());

        GPUUpload upload = new()
        {
            MeshInfo = meshInfo,
            VertexData = vertices.ToArray(),
            IndexData = indices.ToArray(),
            IndexFormat = Format.R32_UInt,
        };
        Context.UploadQueue.Enqueue(upload);

        return meshInfo;
    }
}