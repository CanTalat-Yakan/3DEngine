﻿namespace Engine.Essentials;

public sealed partial class DefaultSky : EditorComponent, IHide
{
    public RootSignature RootSignature;

    public MeshData SkyMeshData;
    public Texture2D SkyGradientTexture;
    public Texture2D SkyGradientLightTexture;

    public override void OnUpdate()
    {
        var camera = Camera.CurrentRenderingCamera;

        Entity.Transform.LocalPosition = camera.Entity.Transform.LocalPosition;
        Entity.Transform.LocalScale = new Vector3(-1, 1, 1) * camera.Clipping.Y;
    }

    public void Initialize()
    {
        Entity.Data.Name = "Sky";
        Entity.Data.Tag = "DefaultSky";
        Entity.Data.IsHidden = true;

        var mesh = Entity.AddComponent<Mesh>();
        mesh.SetMeshData(ModelFiles.Cube);
        mesh.Material.SetRootSignature();
        mesh.Material.SetTextures(TextureFiles.Default);
        mesh.Material.SetPipeline(ShaderFiles.Sky);
        mesh.Order = 0;
    }
}