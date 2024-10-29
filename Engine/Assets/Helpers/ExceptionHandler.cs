using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Engine.Helpers;

public class ExceptionHandler
{
    public static void CreateTraceLog(string rootPath, string logFilePath)
    {
        // Create directory.
        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);

        // Increment log if it is locked by another process.
        bool? isLocked = logFilePath.IsFileLocked();
        if (isLocked is not null && isLocked.Value)
            logFilePath = logFilePath.IncrementPathIfExists(
                Directory.GetFiles(rootPath)
                    .Select(Path.GetFileNameWithoutExtension)
                    .ToArray());

        // Reset log.
        if (File.Exists(logFilePath))
            File.WriteAllText(logFilePath, String.Empty);

        // Set up listener.
        FileStream fileStreamTraceLog = new(logFilePath, FileMode.OpenOrCreate);
        TextWriterTraceListener listener = new(fileStreamTraceLog);

        // Pass listener to trace.
        Trace.Listeners.Add(listener);
        // Automatically write into file.
        Trace.AutoFlush = true;
    }

    public static void HandleException(Exception exception)
    {
        // Write file name, line number, and method name.
        StackTrace stackTrace = new(exception, true);
        StackFrame stackFrame = stackTrace.GetFrame(0); // Get the top frame (most recent method call).

        string fileName = stackFrame.GetFileName();
        int lineNumber = stackFrame.GetFileLineNumber();
        string methodName = stackFrame.GetMethod().Name;

        StringBuilder stringBuilder = new();

        stringBuilder.Append($"[{DateTime.Now}] [ERR] ");
        if (fileName is not null)
            stringBuilder.Append($"{fileName}:{lineNumber} ({methodName})");
        stringBuilder.AppendLine("\n" + exception);

        string OutputMessage = stringBuilder.ToString();

        Debug.WriteLine(OutputMessage);
        Console.WriteLine(OutputMessage);

        Output.Log(exception, MessageType.Error, lineNumber, methodName, fileName, false);
    }
}
