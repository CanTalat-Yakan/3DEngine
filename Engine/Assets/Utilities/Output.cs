using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Engine.Utilities;

public enum MessageType
{
    Message,
    Warning,
    Error
}

public record MessageLog(
    object o,
    MessageType type,
    int line,
    string method,
    string script)
{
    public override string ToString() =>
        GetDateTime() + GetMessageType() + GetOrigin() + GetString();

    public string GetDateTime() =>
        $"[{DateTime.Now}] ";

    public string GetMessageType() =>
        type switch
        {
            MessageType.Message => "[MSG] ",
            MessageType.Warning => "[WRN] ",
            MessageType.Error   => "[ERR] ",
            _ => string.Empty
        };

    public string GetOrigin() =>
        $"{script?.SplitLast('\\')} ({method}:{line})";

    public string GetString() =>
        $"\n{o}\n";
}

public class Output
{
    public static Queue<MessageLog> GetLogs => _logs;
    private static Queue<MessageLog> _logs = new();

    public static void Log(
        object o,
        MessageType type = MessageType.Message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string method = null,
        [CallerFilePath] string script = null)
    {
        MessageLog log = new(o, type, line, method, script);

        Debug.WriteLine(log);
        Console.WriteLine(log);

        _logs.Enqueue(log);
    }

    public static MessageLog DequeueLog()
    {
        if (_logs.Count == 0)
            return null;

        return _logs.Dequeue();
    }

    public static string DequeueLogs()
    {
        if (_logs.Count == 0)
            return null;

        StringBuilder stringBuilder = new();
        while (_logs.Count > 0)
            stringBuilder.Append(_logs.Dequeue());

        return stringBuilder.ToString();
    }
}