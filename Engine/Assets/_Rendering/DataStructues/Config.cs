namespace Engine.DataStructures;

public class Config
{
    public WindowData WindowData;

    public CameraProjection CameraProjection = CameraProjection.Perspective;
    public RenderMode RenderMode = RenderMode.Shaded;

    public PresentInterval VSync = PresentInterval.Immediate;

    public double ResolutionScale = 1;

    public MultiSample MultiSample = MultiSample.None;
    public int SupportedSampleCount = 1;
    public int QualityLevels = 0;

    public bool Debug = true;
    public bool GUI = true;

    public static Config GetDefault(
        PresentInterval presentInterval = PresentInterval.Immediate,
        MultiSample multiSample = MultiSample.x2,
        double resolutionScale = 1,
        string title = "3D Engine", 
        int width = 1080, 
        int height = 720)
    {
        Config config = new();
        config.SetVSync(presentInterval);
        config.SetMSAA(multiSample);
        config.SetResolutionScale(resolutionScale);
        config.SetWindowData(title, width, height);

        return config;
    }

    public void SetWindowData(string title, int width, int height) =>
        WindowData = new(title, width, height);

    public void SetMSAA(MultiSample multiSample) =>
        MultiSample = multiSample;

    public void SetVSync(PresentInterval interval) =>
        VSync = interval;

    public void SetResolutionScale(double scale) =>
        ResolutionScale = scale;

}

public struct WindowData(string title, int width, int height)
{
    public string Title = title;
    public int Width = width;
    public int Height = height;
}

public enum CameraProjection
{
    Perspective,
    Orthographic
}

public enum RenderMode
{
    Shaded,
    Wireframe,
    ShadedWireframe
}

public enum PresentInterval
{
    Immediate,
    Default,
    One,
    Two,
    Three,
    Four
}

public enum MultiSample
{
    None = 1,
    x2 = 2,
    x4 = 4,
    x8 = 8,
}