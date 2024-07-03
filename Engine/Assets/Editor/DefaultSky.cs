namespace Engine.Editor;

public sealed partial class DefaultSky : EditorComponent, IHide
{
    public RootSignature RootSignature;

    public MeshInfo SkyMeshInfo;
    public Texture2D SkyGradientTexture;
    public Texture2D SkyGradientLightTexture;

    public CommonContext Context => _context ??= Kernel.Instance.Context;
    public CommonContext _context;

    public override void OnUpdate()
    {
        var camera = Camera.CurrentRenderingCamera;

        Entity.Transform.LocalPosition = camera.Entity.Transform.LocalPosition;
        Entity.Transform.LocalScale = new Vector3(-1, 1, 1) * 1.9f * camera.Clipping.Y;
    }

    public void Initialize()
    {
        Entity.Name = "Sky";
        Entity.Tag = "DefaultSky";
        Entity.IsHidden = true;

        var mesh = Entity.AddComponent<Mesh>();
        mesh.SetMeshInfo(Assets.Meshes["Sphere.obj"]);
        mesh.SetMaterialTextures([new("Default.png", 0)]);
        mesh.SetMaterialPipeline("Sky");
    }
}