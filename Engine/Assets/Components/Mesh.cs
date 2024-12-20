﻿using System.IO;
using System.Linq;

using Vortice.Mathematics;

namespace Engine.Components;

public sealed partial class Mesh : EditorComponent
{
    public static MeshData CurrentMeshDataOnGPU { get; set; }
    public static Material CurrentMaterialOnGPU { get; set; }

    public MeshData MeshData { get; private set; }
    public Material Material { get; private set; } = new();

    public BoundingBox TransformedBoundingBox { get; private set; }
    public bool InBounds { get; set; } = true;

    [Hide] public CommonContext Context => _context ??= Kernel.Instance.Context;
    private CommonContext _context;

    [Hide] public GraphicsContext GraphicsContext => _graphicsContext ??= Kernel.Instance.Context.GraphicsContext;
    private GraphicsContext _graphicsContext;

    public override void OnRegister() =>
        MeshSystem.Register(this);

    public string MeshPath;
    public string ShaderName;
    public override void OnUpdate()
    {
        if (!string.IsNullOrEmpty(MeshPath))
            if (File.Exists(MeshPath))
                try { SetMeshData(ModelLoader.LoadFile(MeshPath)); }
                finally { MeshPath = null; }

        if (!string.IsNullOrEmpty(ShaderName))
            try { Material.SetPipeline(ShaderName); }
            finally { ShaderName = null; }
    }

    public override void OnRender()
    {
        if (!Context.IsRendering)
            return;

        if (MeshData is null || Material is null)
            return;

        if (!MeshData.IsValid())
            return;

        if (!InBounds)
            return;

        if (MeshData.Equals(CurrentMeshDataOnGPU) && Material.Equals(CurrentMaterialOnGPU))
        {
            Context.UploadBuffer.Upload(Entity.Transform.GetConstantBuffer(), out var offset);
            Context.GraphicsContext.SetConstantBufferView(offset, 1);

            Context.GraphicsContext.DrawIndexedInstanced(MeshData.IndexCount, 1, 0, 0, 0);
        }
        else
        {
            if (CurrentMaterialOnGPU is null || !Material.Equals(CurrentMaterialOnGPU))
                Material.Setup();

            Context.GraphicsContext.SetMesh(MeshData);

            Context.UploadBuffer.Upload(Entity.Transform.GetConstantBuffer(), out var offset);
            Context.GraphicsContext.SetConstantBufferView(offset, 1);

            Context.GraphicsContext.DrawIndexedInstanced(MeshData.IndexCount, 1, 0, 0, 0);

            CurrentMeshDataOnGPU = MeshData;
            CurrentMaterialOnGPU = Material;
        }

        Profiler.Vertices += MeshData.VertexCount;
        Profiler.Indices += MeshData.IndexCount;
        Profiler.DrawCalls++;
    }

    public override void OnDestroy()
    {
        UnsubscribeCheckBounds();

        MeshData?.Dispose();
    }
}

public sealed partial class Mesh : EditorComponent
{
    public void SetMeshData(float[] vertices, int[] indices, Vector3[] positions, InputLayoutHelper inputLayoutElements, string meshName = null, bool unsignedInt32IndexFormat = true) =>
        SetMeshData(Context.CreateMeshData(vertices, indices, positions, inputLayoutElements, meshName, unsignedInt32IndexFormat));

    public void SetMeshData(ModelFiles modelFile) =>
        SetMeshData(Assets.Meshes[modelFile + ".obj"]);

    public void SetMeshData(MeshData meshData)
    {
        if (meshData is null)
            return;

        MeshData = meshData;
        Order = (byte)Array.IndexOf(Assets.Meshes.Values.ToArray(), meshData);

        InstantiateBounds(meshData.BoundingBox);
        SubscribeCheckBounds();
    }

    private void InstantiateBounds(BoundingBox boundingBox)
    {
        MeshData.BoundingBox = boundingBox;

        TransformedBoundingBox = BoundingBox.Transform(
            MeshData.BoundingBox,
            Entity.Transform.WorldMatrix);
    }
}

public sealed partial class Mesh : EditorComponent
{
    private void SubscribeCheckBounds()
    {
        CheckBounds();

        Entity.Transform.TransformChanged += CheckBounds;

        Camera.CurrentRenderingCamera.Entity.Transform.TransformChanged += CheckBounds;
        Camera.CameraChanged += ReplaceCameraCheckBounds;
    }

    private void UnsubscribeCheckBounds()
    {
        Entity.Transform.TransformChanged -= CheckBounds;

        Camera.CurrentRenderingCamera.Entity.Transform.TransformChanged -= CheckBounds;
    }

    private void ReplaceCameraCheckBounds()
    {
        CheckBounds();

        Camera.CurrentRenderingCamera.Entity.Transform.TransformChanged += CheckBounds;

        if (Camera.PreviousRenderingCamera is not null)
            Camera.PreviousRenderingCamera.Entity.Transform.TransformChanged -= CheckBounds;
    }

    private void CheckBounds()
    {
        TransformedBoundingBox = BoundingBox.Transform(
            MeshData.BoundingBox,
            Entity.Transform.WorldMatrix);

        var boundingFrustum = Camera.CurrentRenderingCamera.BoundingFrustum;
        if (boundingFrustum is not null)
            InBounds = boundingFrustum.Value.Intersects(TransformedBoundingBox);
    }
}