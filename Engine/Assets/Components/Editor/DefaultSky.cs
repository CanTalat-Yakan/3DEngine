using Engine.Loader;

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
        LoadResources();

        Entity.Name = "Sky";
        Entity.Tag = "DefaultSky";
        Entity.IsHidden = true;

        Entity.Transform.LocalScale = new Vector3(-100, 100, 100);

        var mesh = Entity.GetComponent<Mesh>();
        mesh.SetMeshInfo(SkyMeshInfo);
        mesh.SetMaterialTexture(new MaterialTextureEntry("SkyGradient.png", 0));
        mesh.Material.SetPipelineStateObject("Unlit");
    }
}

public sealed partial class DefaultSky : EditorComponent, IHide
{
    public void LoadResources()
    {
        RootSignature = Context.CreateRootSignatureFromString("Cs");

        SkyMeshInfo = ModelLoader.LoadFile(Paths.PRIMITIVES + "Sphere.obj");
        SkyGradientTexture = ImageLoader.LoadTexture(Paths.TEXTURES + "SkyGradient.png");
        SkyGradientLightTexture = ImageLoader.LoadTexture(Paths.TEXTURES + "SkyGradient_Light.png");
    }
}
