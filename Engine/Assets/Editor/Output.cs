using System.Runtime.CompilerServices;
//using EditorOutput = Editor.Controller.Output;
//using EditorMessageType = Editor.Controller.MessageType;

namespace Engine;

public enum MessageType
{
    Message,
    Warning,
    Error
}

internal class Output
{
    public static void Log(object o, MessageType t = MessageType.Message, [CallerLineNumber] int l = 0, [CallerMemberName] string c = null, [CallerFilePath] string s = null) { }
        //EditorOutput.Log(o.ToString(), (EditorMessageType)(int)t, l, c, s);
}
