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

        // Set the Skybox position to the rendering camera position.
        Entity.Transform.LocalPosition = camera.Entity.Transform.LocalPosition;
        // Set the Skybox scale to the rendering camera far clipping plane.
        Entity.Transform.LocalScale = new Vector3(-1, 1, 1) * 1.9f * camera.Clipping.Y;
    }

    public void Initialize()
    {
        LoadResources();

        Entity.Name = "Sky";
        Entity.Tag = "DefaultSky";
        Entity.IsHidden = true;

        Entity.Transform.LocalScale = new Vector3(-100, 100, 100);

        var mesh = Entity.AddComponent<Mesh>();
        mesh.SetMeshInfo(SkyMeshInfo);
        mesh.SetMaterialTexture(new MaterialTextureEntry("SkyGradient.png", 0));
    }
}

public sealed partial class DefaultSky : EditorComponent, IHide
{
    public void LoadResources()
    {
        Context.VertexShaders["SimpleLit"] = Context.GraphicsContext.LoadShader(DxcShaderStage.Vertex, Paths.SHADERS + "SimpleLit.hlsl", "VS");
        Context.PixelShaders["SimpleLit"] = Context.GraphicsContext.LoadShader(DxcShaderStage.Pixel, Paths.SHADERS + "SimpleLit.hlsl", "PS");

        Context.PipelineStateObjects["SimpleLit"] = new PipelineStateObject(Context.VertexShaders["SimpleLit"], Context.PixelShaders["SimpleLit"]);

        RootSignature = Context.CreateRootSignatureFromString("Cs");

        SkyMeshInfo = ModelLoader.LoadFile(Paths.PRIMITIVES + "Sphere.obj", "PNTt");
        SkyGradientTexture = ImageLoader.LoadTexture(Paths.TEXTURES + "SkyGradient.png");
        SkyGradientLightTexture = ImageLoader.LoadTexture(Paths.TEXTURES + "SkyGradient_Light.png");
    }

    public void SetTheme(bool light)
    {
        //// Switch the material of the sky component to either the default or light sky material.
        //Entity.GetComponent<Mesh>().SetMaterial(light ? _materialSkyLight : _materialSky);
    }
}
