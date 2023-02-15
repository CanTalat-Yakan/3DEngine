namespace Editor.Controller;

internal class Profiler
{
    public static int FPS => Time.FPS;
    public static double Delta => Time.Delta;

    public static double SwapChainSizeWidth => Renderer.Instance.SwapChainPanel.ActualWidth;
    public static double SwapChainSizeHeight => Renderer.Instance.SwapChainPanel.ActualHeight;

    public static float DrawCalls { get; set; }
    public static float Vertices { get; set; }
    public static float Indices { get; set; }

    public static string GetString() =>
        $"""
            {(int)(Delta * 1000)} ms
            {FPS} FPS
            
            Resolution: 
            {SwapChainSizeWidth + ":" + SwapChainSizeHeight}
            
            DrawCalls: {DrawCalls}
            Vertices: {Vertices}
            Indices: {Indices}
            """;
}
