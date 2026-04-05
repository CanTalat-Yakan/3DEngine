using UltralightNet.Platform;

namespace UltralightNet.Enums;

/// <summary>
/// Log levels, used with <see cref="ILogger.LogMessage"/>
/// </summary>
public enum LogLevel : byte
{
	Error,
	Warning,
	Info
}
