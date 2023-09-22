using System.Diagnostics;

namespace Engine.Utilities;

public sealed class Time
{
    public static double Timer => s_timer;
    public static double Delta => s_delta;
    public static float DeltaF => (float)s_delta;
    public static int FPS => s_fps;
    public static double TimeStep => s_timeStep;
    public static double TimeScale => s_timeScale;
    public static bool TimeStepEllapsed => s_timeStepCounter == 0;

    private static double s_timer, s_delta;
    private static int s_fps, s_tmpFPS;

    private static Stopwatch s_watch = new();
    private static DateTime s_now = DateTime.Now;

    private static double s_timeScale = 1;

    private static double s_timeStep = 1 / 50;
    private static double s_timeStepCounter;

    public static void Update()
    {
        // Calculates the elapsed time by dividing the elapsed milliseconds by 1000.
        s_delta = s_watch.Elapsed.TotalSeconds;

        // Adds the elapsed time to the running total time.
        s_timer += s_delta;

        // Muultiply the delta time witht the time scale.
        s_delta *= s_timeScale;

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


        // Check for FixedUpdate with the timeStepCounter
        if (s_timeStepCounter < TimeStep)
            s_timeStepCounter += Delta;
        else
            s_timeStepCounter = 0;
    }

    public static void SetTimeStep(float timeStep) =>
        s_timeStep = timeStep;

    public static void SetTimeScale(float timeScale) =>
        s_timeScale = timeScale;
}
