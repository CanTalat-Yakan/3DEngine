using System;
using System.Diagnostics;

namespace Engine.Utilities
{
    internal class Time
    {
        public string Profile = "";

        public static double s_Time, s_Delta;
        public static Stopwatch s_Watch = new Stopwatch();

        private int _fps, _tmpFPS;
        private DateTime _now = DateTime.Now;

        public void Update()
        {
            s_Delta = s_Watch.ElapsedMilliseconds * 0.001;
            s_Time += s_Delta;
            ++_tmpFPS;

            if (_now.Second != DateTime.Now.Second)
            {
                _fps = _tmpFPS;
                _tmpFPS = 0;
                _now = DateTime.Now;

                Profile = s_Watch.ElapsedMilliseconds.ToString() + " ms" + "\n" + _fps.ToString() + " FPS";
            }

            s_Watch.Restart();
        }
    }
}
