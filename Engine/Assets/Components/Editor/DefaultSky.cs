using Engine.Loader;
using System.IO;
using Vortice.Dxc;

namespace Engine.Editor;

public class DefaultSky : EditorComponent, IHide
{
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
        //// Create a new material with the unlit shader and sky image.
        //_materialSky = new(SHADER_RESOURCES + SHADER_SIMPLE_LIT, IMAGE_SKY);
        //// Create a new material with the unlit shader and a light version of the sky image.
        //_materialSkyLight = new(SHADER_RESOURCES + SHADER_SIMPLE_LIT, IMAGE_SKY_LIGHT);
        LoadDefaultResource();

        Entity.Name = "Sky";
        Entity.Tag = "DefaultSky";
        Entity.IsHidden = true;

        // Set scale of the Sky's transform.
        Entity.Transform.LocalScale = new Vector3(-100, 100, 100);

        // Add Mesh component to Sky entity.
        var skyMesh = Entity.AddComponent<Mesh>();
        //skyMesh.SetMeshInfo(ModelLoader.LoadFile(Path.Combine("Primitives", PrimitiveTypes.Sphere.ToString()) + ".obj"));

        // Set material of Sky's Mesh component.
        //skyMesh.SetMaterial(_materialSky);
    }

    public void LoadDefaultResource()
    {
        Context.VertexShaders["SimpleLit"] = Context.GraphicsContext.LoadShader(DxcShaderStage.Vertex, Paths.SHADERS + "SimpleLit.hlsl", "VS");
        Context.PixelShaders["SimpleLit"] = Context.GraphicsContext.LoadShader(DxcShaderStage.Pixel, Paths.SHADERS + "SimpleLit.hlsl", "PS");

        Context.PipelineStateObjects["SimpleLit"] = new PipelineStateObject(Context.VertexShaders["SimpleLit"], Context.PixelShaders["SimpleLit"]); ;

        //ImageLoader.LoadTexture(out var texture2D, Context.GraphicsDevice.Device, Paths.TEXTURES + "SkyGradient.png");
    }

    public void SetTheme(bool light)
    {
        //// Switch the material of the sky component to either the default or light sky material.
        //Entity.GetComponent<Mesh>().SetMaterial(light ? _materialSkyLight : _materialSky);
    }
}
