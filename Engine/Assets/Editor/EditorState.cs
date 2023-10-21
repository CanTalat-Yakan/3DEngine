namespace Engine.Editor;

public class EditorState
{
    public static bool EditorBuild { get; set; } = false;
    public static string AssetsPath { get; set; }
    public static bool PlayMode { get; set; } = false;
    public static bool PlayModeStarted { get; set; } = false;

    public static void SetPlayMode(bool b) =>
        PlayMode = b;

    public static void SetPlayModeStarted(bool b) =>
        PlayModeStarted = b;
}
