using System.Diagnostics;

namespace Engine.Utilities;

public class Time
{
    public static double Timer => s_timer;
    public static double Delta => s_delta;
    public static int FPS => s_fps;

    private static double s_timer, s_delta;
    private static int s_fps, s_tmpFPS;

    private static Stopwatch s_watch = new();
    private static DateTime s_now = DateTime.Now;

    public static void Update()
    {
        // Calculates the elapsed time by dividing the elapsed milliseconds by 1000.
        s_delta = s_watch.Elapsed.TotalSeconds;

        // Adds the elapsed time to the running total time.
        s_timer += s_delta;

        // Increases the temporary frame count by 1.
        ++s_tmpFPS;

        // Updates the fps value and resets the temporary frame count if a second has passed.
        if (s_now.Second != DateTime.Now.Second)
        {
            s_fps = s_tmpFPS;
            s_tmpFPS = 0;
            s_now = DateTime.Now;
        }

        // Restarts the stopwatch to measure the time for the next frame.
        s_watch.Restart();
    }
}
