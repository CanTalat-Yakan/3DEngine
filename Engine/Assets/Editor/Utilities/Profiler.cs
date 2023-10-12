namespace Engine;

public sealed class Profiler
{
    public static int FPS => Time.FPS;
    public static double Delta => Time.Delta;

    public static double SwapChainSizeWidth => Renderer.Instance.Size.Width;
    public static double SwapChainSizeHeight => Renderer.Instance.Size.Height;

    public static float DrawCalls { get; set; }
    public static float Vertices { get; set; }
    public static float Indices { get; set; }

    public static string GetString() =>
        $"""
        {FPS} FPS ({(int)(Delta * 1000)}ms)
        
        Draw Calls: {DrawCalls}
        Vertices: {Vertices}
        Triangles: {Indices / 3}

        Resolution: {SwapChainSizeWidth + "x" + SwapChainSizeHeight}
        """;
}
