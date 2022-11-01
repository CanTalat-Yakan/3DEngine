using System;
using System.Diagnostics;

namespace Engine.Utilities
{
    internal class Time
    {
        internal string profile = "";

        internal static double s_time, s_delta;
        internal static Stopwatch s_watch = new Stopwatch();
        int fps, tmpFPS;
        DateTime now = DateTime.Now;

        internal void Update()
        {
            s_delta = s_watch.ElapsedMilliseconds * 0.001;
            s_time += s_delta;
            ++tmpFPS;

            if (now.Second != DateTime.Now.Second)
            {
                fps = tmpFPS;
                tmpFPS = 0;
                now = DateTime.Now;

                profile = s_watch.ElapsedMilliseconds.ToString() + " ms" + "\n" + fps.ToString() + " FPS";
            }

            s_watch.Restart();
        }
    }
}
