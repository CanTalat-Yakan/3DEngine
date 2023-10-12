using System.IO;
using System.Linq;

namespace Engine.Editor;

public class DefaultSky : EditorComponent
{
    private static Material _materialSky;
    private static Material _materialSkyLight;

    private static readonly string SHADER_RESOURCES = Path.Combine(AppContext.BaseDirectory, Paths.SHADERS);

    private static readonly string SHADER_SKYBOX = "Skybox.hlsl";

    private static readonly string IMAGE_SKY = "SkyGradient.png";
    private static readonly string IMAGE_SKY_LIGHT = "SkyGradient_Light.png";

    private static readonly string PRIMITIVES = "Primitives";

    public override void OnUpdate()
    {
        var camera = CameraSystem.Components.Last();

        // Set the Skybox position to the rendering camera position.
        Entity.Transform.LocalPosition = camera.Entity.Transform.Position;
        // Set the Skybox scale to the rendering camera far clipping plane.
        Entity.Transform.LocalScale = new Vector3(-1, 1, 1) * 1.9f * camera.Clipping.Y;
    }

    public void Initialize()
    {
        // Create a new material with the unlit shader and sky image.
        _materialSky = new(SHADER_RESOURCES + SHADER_SKYBOX, IMAGE_SKY);
        // Create a new material with the unlit shader and a light version of the sky image.
        _materialSkyLight = new(SHADER_RESOURCES + SHADER_SKYBOX, IMAGE_SKY_LIGHT);

        Entity.Name = "Sky"; // Set entity name to "Sky".
        Entity.Tag = EditorTags.SceneSky.ToString(); // Set entity tag to SceneSky.

        // Set scale of the Sky's transform.
        Entity.Transform.LocalScale = new Vector3(-100, 100, 100);

        // Add Mesh component to Sky entity.
        var skyMesh = Entity.AddComponent<Mesh>();
        skyMesh.SetMeshInfo(Loader.ModelLoader.LoadFile(Path.Combine(PRIMITIVES, PrimitiveTypes.Sphere.ToString()) + ".obj"));

        // Set material of Sky's Mesh component.
        skyMesh.SetMaterial(_materialSky);
    }

    public void SetTheme(bool light) =>
        // Switch the material of the sky component to either the default or light sky material.
        Entity.GetComponent<Mesh>().SetMaterial(light ? _materialSkyLight : _materialSky);
}
