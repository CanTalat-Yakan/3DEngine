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
            s_delta = s_watch.ElapsedMilliseconds * 0.001;
            s_time += s_delta;
            ++_tmpFPS;

            if (_now.Second != DateTime.Now.Second)
            {
                _fps = _tmpFPS;
                _tmpFPS = 0;
                _now = DateTime.Now;
            }

            s_watch.Restart();
        }
    }
}
