using System.Diagnostics;
using System.Text;

namespace Engine;

/// <summary>Installs a simple exception handler that logs to Application.log and writes to Debug/Console.</summary>
public sealed class ExceptionsPlugin : IPlugin
{
    /// <summary>Configures global unhandled exception logging and stores a marker resource to avoid reinstallation.</summary>
    public void Build(App app)
    {
        if (!app.World.ContainsResource<ExceptionHandlerInstalled>())
        {
            Install();
            app.World.InsertResource(new ExceptionHandlerInstalled());
        }
    }

    private static void Install()
    {
        var rootPath = AppContext.BaseDirectory;
        var logFilePath = Path.Combine(rootPath, "Application.log");

        ExceptionHandler.CreateTraceLog(rootPath, logFilePath);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
                ExceptionHandler.HandleException(exception);
        };
    }
}

/// <summary>Marker resource indicating exception handler has been installed.</summary>
public sealed class ExceptionHandlerInstalled { }

/// <summary>Utility for creating a trace log and formatting/printing unhandled exceptions.</summary>
public static class ExceptionHandler
{
    /// <summary>Creates/rotates the log file and hooks up a TextWriterTraceListener.</summary>
    public static void CreateTraceLog(string rootPath, string logFilePath)
    {
        // Create directory.
        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);

        // Increment log if it is locked by another process.
        bool? isLocked = logFilePath.IsFileLocked();
        if (isLocked == true)
        {
            var names = Directory.GetFiles(rootPath)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(n => n is not null)
                .Select(n => n!)
                .ToArray();
            logFilePath = logFilePath.IncrementPathIfExists(names);
        }

        // Reset log.
        if (File.Exists(logFilePath))
            File.WriteAllText(logFilePath, string.Empty);

        // Set up listener.
        var listener = new TextWriterTraceListener(new FileStream(logFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read));

        // Pass listener to trace.
        Trace.Listeners.Add(listener);
        // Automatically write into file.
        Trace.AutoFlush = true;
    }

    /// <summary>Formats and prints an exception with file/line/method when available.</summary>
    public static void HandleException(Exception exception)
    {
        // Write file name, line number, and method name.
        var stackTrace = new StackTrace(exception, true);
        var stackFrame = stackTrace.GetFrame(0); // Get the top frame (most recent method call).

        var fileName = stackFrame?.GetFileName();
        var lineNumber = stackFrame?.GetFileLineNumber() ?? 0;
        var methodName = stackFrame?.GetMethod()?.Name ?? "<unknown>";

        var stringBuilder = new StringBuilder();

        stringBuilder.Append($"[{DateTime.Now}] [ERR] ");
        if (fileName is not null)
            stringBuilder.Append($"{fileName}:{lineNumber} ({methodName})");
        stringBuilder.AppendLine("\n" + exception);

        var outputMessage = stringBuilder.ToString();

        Debug.WriteLine(outputMessage);
        Console.WriteLine(outputMessage);
    }
}

internal static class ExceptionHandlingExtensions
{
    public static string IncrementNameIfExists(this string name, string[] list)
    {
        var i = 0;
        bool nameWithoutIncrement = list.Contains(name);

        foreach (var s in list)
            if (s == name || s.Contains(name + " ("))
                i++;

        if (i > 0 && nameWithoutIncrement)
            name += " (" + (i + 1).ToString() + ")";

        return name;
    }

    public static string IncrementPathIfExists(this string path, string[] list)
    {
        var name = Path.GetFileNameWithoutExtension(path); 

        name = name.IncrementNameIfExists(list);

        var dir = Path.GetDirectoryName(path) ?? string.Empty;
        return Path.Combine(dir, name + Path.GetExtension(path));
    }

    public static bool? IsFileLocked(this string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            var fileInfo = new FileInfo(path);
            using (fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None))
            {
                // If we can open it exclusively, it's not locked
            }
        }
        catch (IOException)
        {
            // the file is unavailable because it is:
            // still being written to
            // or being processed by another thread
            // or does not exist (has already been processed)
            return true;
        }

        // file is not locked
        return false;
    }
}
