using System.Diagnostics;
using System;

namespace Engine.Utilities
{
    internal class Time
    {
        public static double Delta { get => s_delta; }
        public static int FPS { get => _fps; }

        private static double s_time, s_delta;
        private static int _fps, _tmpFPS;

        private static Stopwatch s_watch = new();
        private static DateTime _now = DateTime.Now;

        public static void Update()
        {
            // Calculates the elapsed time by dividing the elapsed milliseconds by 1000.
            s_delta = s_watch.ElapsedMilliseconds * 0.001;

            // Adds the elapsed time to the running total time.
            s_time += s_delta;

            // Increases the temporary frame count by 1.
            ++_tmpFPS;

            // Updates the fps value and resets the temporary frame count if a second has passed.
            if (_now.Second != DateTime.Now.Second)
            {
                _fps = _tmpFPS;
                _tmpFPS = 0;
                _now = DateTime.Now;
            }

            // Restarts the stopwatch to measure the time for the next frame.
            s_watch.Restart();
        }
    }
}
