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
            string profile = "";

            profile += (int)(Delta * 1000) + " ms" + "\n";
            profile += FPS + " FPS" + "\n";
            profile += "\n";
            profile += "Resolution: " + "\n" + SwapChainSizeWidth + ":" + SwapChainSizeHeight + "\n";
            profile += "\n";
            profile += "DrawCalls: " + DrawCalls + "\n";
            profile += "Vertices: " + Vertices + "\n";
            profile += "Indices: " + Indices;

            return profile;
        }
    }
}
