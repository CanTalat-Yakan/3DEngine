namespace Engine.DataStructures;

public class Config
{
    public WindowData WindowData;
    public WindowCommand WindowCommand = WindowCommand.Show;

    public CameraProjection CameraProjection = CameraProjection.Perspective;
    public RenderMode RenderMode = RenderMode.Shaded;

    public PresentInterval VSync = PresentInterval.Immediate;

    public double ResolutionScale = 1;

    public MultiSample MultiSample = MultiSample.None;

    public uint SampleCount = 1;
    public uint SampleQuality = 0;

    public bool Debug = true;
    public bool GUI = true;
    public bool Boot = true;

    public static Config GetDefault(
        WindowCommand windowCommand = WindowCommand.Show,
        PresentInterval presentInterval = PresentInterval.Immediate,
        MultiSample multiSample = MultiSample.None,
        double resolutionScale = 1,
        string title = "3D Engine", 
        int width = 1080, 
        int height = 720,
        bool renderGUI = true,
        bool defaultBoot = false)
    {
        Config config = new();
        config.SetWindowData(title, width, height);
        config.SetWindowCommand(windowCommand);
        config.SetVSync(presentInterval);
        config.SetMSAA(multiSample);
        config.SetResolutionScale(resolutionScale);

        config.GUI = renderGUI;
        config.Boot = defaultBoot;

        return config;
    }

    public void SetWindowData(string title, int width, int height) =>
        WindowData = new(title, width, height);

    public void SetWindowCommand(WindowCommand windowCommand) =>
        WindowCommand = windowCommand;

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

public enum MultiSample : uint
{
    None = 1,
    x2 = 2,
    x4 = 4,
    x8 = 8,
}

public enum WindowCommand
{
    Hide = 0,
    Normal = 1,
    ShowMinimized = 2,
    Maximize = 3,
    ShowMaximized = 3,
    ShowNoActivate = 4,
    Show = 5,
    Minimize = 6,
    ShowMinNoActive = 7,
    ShowNA = 8,
    Restore = 9,
    ShowDefault = 10,
    ForceMinimize = 11
}