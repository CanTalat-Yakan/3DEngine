using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
    public string GetString() =>
        $"{type}: {o}\n{script.SplitLast('\\')}({method}:{line})\n\n"  ;
}

public class Output
{
    public static Queue<MessageLog> GetLogs  => _logs;
    private static Queue<MessageLog> _logs = new();

    public static void Log(object o, MessageType type = MessageType.Message, [CallerLineNumber] int line = 0, [CallerMemberName] string method = null, [CallerFilePath] string script = null) =>
        _logs.Enqueue(new(o, type, line, method, script));

    public static MessageLog DequeueLog()
    {
        if( _logs.Count > 0 )
            return _logs.Dequeue();
        else 
            return null;
    }
}
