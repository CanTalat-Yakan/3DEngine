﻿using System.IO;
using System.Runtime.CompilerServices;

using Vortice.Direct3D11;
using Vortice.Direct3D;
using Engine.Data;

namespace Engine.Components;

public sealed class Mesh : Component
{
    public string MeshPath;

    public static MeshInfo CurrentMeshOnGPU { get; private set; }

    public MeshInfo MeshInfo => _meshInfo;
    [Show] private MeshInfo _meshInfo;

    public Material Material => _material;
    [Show] private Material _material;

    internal ID3D11Buffer VertexBuffer;
    internal ID3D11Buffer IndexBuffer;

    internal int VertexCount => _meshInfo.Vertices.Length;
    internal int VertexStride => Unsafe.SizeOf<Vertex>();
    internal int IndexCount => _meshInfo.Indices.Length;
    internal int IndexStride => Unsafe.SizeOf<int>();

    private Renderer _renderer => Renderer.Instance;

    public override void OnRegister() =>
        // Register the component with the MeshSystem.
        MeshSystem.Register(this);

    public Mesh()
    {
        SetMeshInfo(EntityManager.GetDefaultMeshInfo());
        SetMaterial(EntityManager.GetDefaultMaterial());
    }

    public override void OnUpdate()
    {
        if (!string.IsNullOrEmpty(MeshPath))
            if (File.Exists(MeshPath))
                try
                {
                    SetMeshInfo(Loader.ModelLoader.LoadFile(MeshPath, false));
                }
                finally
                {
                    MeshPath = null;
                }
    }

    public override void OnRender()
    {
        if (Equals(Material.CurrentMaterialOnGPU, _material)
            && Equals(Mesh.CurrentMeshOnGPU, _meshInfo))
        {
            _material.UpdateConstantBuffer(Entity.Transform.GetConstantBuffer());

            _renderer.DrawDirect(IndexCount);
        }
        else
        {
            // Set the material's constant buffer to the entity's transform constant buffer.
            _material.Set(Entity.Transform.GetConstantBuffer());

            // Draw the mesh with trianglelist.
            _renderer.Data.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _renderer.Draw(VertexBuffer, IndexBuffer, IndexCount, VertexStride, 0, 0);

            // Assign meshInfo to the static variable.
            CurrentMeshOnGPU = _meshInfo;
        }

        // Increment the vertex, index and draw call count in the profiler.
        Profiler.Vertices += VertexCount;
        Profiler.Indices += IndexCount;
        Profiler.DrawCalls++;
    }

    public void SetMeshInfo(MeshInfo meshInfo)
    {
        // Assign to local variable.
        _meshInfo = meshInfo;

        // Call the "CreateBuffer" method to initialize the vertex and index buffer.
        CreateBuffer();
    }

    public void SetMaterial(Material material) =>
        // Assign to local variable.
        _material = material;

    public void Dispose()
    {
        VertexBuffer?.Dispose();
        IndexBuffer?.Dispose();
    }

    private void CreateBuffer()
    {
        Dispose();

        //Create a VertexBuffer using the MeshInfo's vertices
        //and bind it with VertexBuffer flag.
        VertexBuffer = _renderer.Device.CreateBuffer(
            _meshInfo.Vertices,
            BindFlags.VertexBuffer);

        //Create an IndexBuffer using the MeshInfo's indices
        //and bind it with IndexBuffer flag.
        IndexBuffer = _renderer.Device.CreateBuffer(
            _meshInfo.Indices,
            BindFlags.IndexBuffer);
    }
}
