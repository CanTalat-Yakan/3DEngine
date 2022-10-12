using System;
using System.Diagnostics;

namespace WinUI3DEngine.Assets.Engine.Utilities
{
    internal class CTime
    {
        internal string m_Profile = "";

        internal static double m_Time, m_Delta;
        internal static Stopwatch m_Watch = new Stopwatch();
        int m_fps, m_lastFPS;
        DateTime m_now = DateTime.Now;

        internal void Update()
        {
            m_Delta = m_Watch.ElapsedMilliseconds * 0.001;
            m_Time += m_Delta;
            ++m_lastFPS;

            if (m_now.Second != DateTime.Now.Second)
            {
                m_fps = m_lastFPS;
                m_lastFPS = 0;
                m_now = DateTime.Now;

                m_Profile = m_Watch.ElapsedMilliseconds.ToString() + " ms" + "\n" + m_fps.ToString() + " FPS";
            }

            m_Watch.Restart();
        }
    }
}
