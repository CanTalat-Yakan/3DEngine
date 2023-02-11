using Engine.Utilities;

namespace Editor.Controller
{
    internal class Profiler
    {
        public static int FPS { get => Time.FPS; }
        public static double Delta { get => Time.Delta; }

        public static double SwapChainSizeWidth { get => Renderer.Instance.SwapChainPanel.ActualWidth; }
        public static double SwapChainSizeHeight { get => Renderer.Instance.SwapChainPanel.ActualHeight; }

        public static float DrawCalls { get; set; }
        public static float Vertices { get; set; }
        public static float Indices { get; set; }

        public static string ToString()
        {
            return $"""
                {(int)(Delta * 1000)} ms
                {FPS} FPS
                
                Resolution: 
                {SwapChainSizeWidth + ":" + SwapChainSizeHeight}
                
                DrawCalls: {DrawCalls}
                Vertices: {Vertices}
                Indices: {Indices}
                """;
        }
    }
}
