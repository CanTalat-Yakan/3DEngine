using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Engine;

public enum MessageType
{
    Message,
    Warning,
    Error
}

public record MessageLog(
    object o, 
    MessageType t, 
    int l, 
    string c, 
    string s);

public class Output
{
    public static Queue<MessageLog> GetLogs  => _logs;
    private static Queue<MessageLog> _logs = new();

    public static void Log(object o, MessageType t = MessageType.Message, [CallerLineNumber] int l = 0, [CallerMemberName] string c = null, [CallerFilePath] string s = null) =>
        _logs.Enqueue(new(o, (MessageType)(int)t, l, c, s));

    public static MessageLog DequeueLog()
    {
        if( _logs.Count > 0 )
            return _logs.Dequeue();
        else 
            return null;
    }
}
