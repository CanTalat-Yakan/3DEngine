﻿using System.Diagnostics;
using System.Text;

namespace Engine.Utilities;

public sealed class Profiler
{
    public static int FPS => Time.FPS;
    public static double Delta => Time.Delta;

    public static double ViewportSizeWidth => Renderer.Instance.Size.Width;
    public static double ViewportSizeHeight => Renderer.Instance.Size.Height;

    public static float DrawCalls { get; set; }
    public static float Vertices { get; set; }
    public static float Indices { get; set; }

    public static StringBuilder AdditionalProfiling { get; set; } = new();

    public static void Start(out Stopwatch stopwatch)
    {
        stopwatch = new();
        stopwatch.Start();
    }

    public static void Stop(Stopwatch stopwatch, string name)
    {
        stopwatch.Stop();

        var delta = stopwatch.Elapsed.TotalSeconds;
        AdditionalProfiling.Append($"{name}: {delta * 1000:F2} ms\n");
    }

    public static void Reset() =>
        AdditionalProfiling.Clear();

    public static string GetString() =>
        $"""
        {FPS} FPS ({Delta * 1000:F2} ms)
        
        Draw Calls: {DrawCalls}
        Vertices: {Vertices}
        Triangles: {Indices / 3}

        Resolution: {ViewportSizeWidth + "x" + ViewportSizeHeight}
        """;

    public static string GetAdditionalString() =>
        GetString() + "\n\n" + AdditionalProfiling.ToString();
}
