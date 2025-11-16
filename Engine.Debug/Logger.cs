namespace Engine;

public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

public interface ILoggerProvider
{
    void Log(LogLevel level, string category, string message, Exception? exception = null);
}

public interface ILogger
{
    void Log(LogLevel level, string message, Exception? exception = null);
    void Trace(string message);
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
    void Critical(string message, Exception? exception = null);
}

public sealed class Logger : ILogger
{
    private readonly string _category;
    private readonly List<ILoggerProvider> _providers = new();

    public Logger(string category)
    {
        _category = category;
        _providers.Add(ConsoleLoggerProvider.Instance);
    }

    public Logger UseProvider(ILoggerProvider provider)
    {
        if (!_providers.Contains(provider))
            _providers.Add(provider);
        return this;
    }

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        foreach (var provider in _providers)
            provider.Log(level, _category, message, exception);
    }

    public void Trace(string message) => Log(LogLevel.Trace, message);
    public void Debug(string message) => Log(LogLevel.Debug, message);
    public void Info(string message) => Log(LogLevel.Info, message);
    public void Warn(string message) => Log(LogLevel.Warning, message);
    public void Error(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);
    public void Critical(string message, Exception? exception = null) => Log(LogLevel.Critical, message, exception);
}

public sealed class LoggerFactory
{
    private readonly Dictionary<string, Logger> _loggers = new();

    public Logger CreateLogger(string category)
    {
        if (_loggers.TryGetValue(category, out var existing))
            return existing;
        var logger = new Logger(category);
        _loggers[category] = logger;
        return logger;
    }
}

public sealed class ConsoleLoggerProvider : ILoggerProvider
{
    public static ConsoleLoggerProvider Instance { get; } = new();
    private ConsoleLoggerProvider() { }

    public void Log(LogLevel level, string category, string message, Exception? exception = null)
    {
        var timestamp = DateTime.UtcNow.ToString("O");
        var levelTag = level.ToString().ToUpperInvariant();
        Console.WriteLine($"[{timestamp}] [{levelTag}] [{category}] {message}");
        if (exception != null)
            Console.WriteLine(exception);
    }
}

public static class Log
{
    public static LoggerFactory Factory { get; } = new();
    public static ILogger Category(string category) => Factory.CreateLogger(category);
    public static ILogger For<T>() => Category(typeof(T).FullName ?? typeof(T).Name);
}
