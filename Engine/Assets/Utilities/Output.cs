﻿using System.Collections.Concurrent;
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
    object obj,
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
            MessageType.Error => "[ERR] ",
            _ => string.Empty
        };

    public string GetOrigin() =>
        $"{script?.SplitLast('\\')} ({method}:{line})";

    public string GetString() =>
        $"\n{obj}\n";
}

public class Output
{
    public static ConcurrentQueue<MessageLog> GetLogs => _logs;
    private static ConcurrentQueue<MessageLog> _logs = new();

    public static void Log(
        object obj,
        MessageType type = MessageType.Message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string method = null,
        [CallerFilePath] string script = null,
        bool writeLineDebugConsole = true)
    {
        MessageLog log = new(obj, type, line, method, script);

        if (writeLineDebugConsole)
        {
            Debug.WriteLine(log);
            Console.WriteLine(log);
        }

        _logs.Enqueue(log);
    }

    public static MessageLog DequeueLog()
    {
        if (_logs.Count == 0)
            return null;

        _logs.TryDequeue(out var result);

        return result;
    }

    public static string DequeueLogs()
    {
        if (_logs.Count == 0)
            return null;

        StringBuilder stringBuilder = new();
        while (_logs.Count > 0)
        {
            _logs.TryDequeue(out var result);
            if (result is not null)
                stringBuilder.Append(result.GetString());
        }

        return stringBuilder.ToString();
    }
}