using System.Diagnostics;
using System.Text;

namespace Engine;

public static class ExceptionHandler
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
    }
}

public static class ExtensionMethods
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

        return Path.Combine(Path.GetDirectoryName(path), name + Path.GetExtension(path));
    }

    public static bool? IsFileLocked(this string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            FileInfo fileInfo = new FileInfo(path);
            using (FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                fileStream.Close();
        }
        catch (IOException)
        {
            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            return true;
        }

        //file is not locked
        return false;
    }
}