using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Engine;

/// <summary>Severity levels for log messages.</summary>
public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>Sink that receives formatted log messages.</summary>
public interface ILoggerProvider
{
    void Log(LogLevel level, string category, string message, Exception? exception = null);
}

/// <summary>Category-scoped logging interface.</summary>
public interface ILogger
{
    void Log(LogLevel level, string message, Exception? exception = null);
    void Trace(string message);
    /// <summary>Trace-level log that only emits when <see cref="LogConfig.PerFrameLogging"/> is enabled. Use for per-frame repetitive diagnostics (stage timing, render steps, etc.).</summary>
    void FrameTrace(string message);
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
    void Critical(string message, Exception? exception = null);
}

/// <summary>Default logger implementation that dispatches to one or more providers.</summary>
public sealed class Logger : ILogger
{
    private readonly string _category;
    private readonly List<ILoggerProvider> _providers = new();

    public Logger(string category)
    {
        _category = category;
        _providers.Add(ConsoleLoggerProvider.Instance);
        if (FileLoggerProvider.Instance is { } file)
            _providers.Add(file);
    }

    public Logger UseProvider(ILoggerProvider provider)
    {
        if (!_providers.Contains(provider))
            _providers.Add(provider);
        return this;
    }

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        if (level < LogConfig.MinimumLevel) return;
        foreach (var provider in _providers)
            provider.Log(level, _category, message, exception);
    }

    public void Trace(string message) => Log(LogLevel.Trace, message);
    public void FrameTrace(string message) { if (LogConfig.PerFrameLogging) Log(LogLevel.Trace, message); }
    public void Debug(string message) => Log(LogLevel.Debug, message);
    public void Info(string message) => Log(LogLevel.Info, message);
    public void Warn(string message) => Log(LogLevel.Warning, message);
    public void Error(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);
    public void Critical(string message, Exception? exception = null) => Log(LogLevel.Critical, message, exception);
}

/// <summary>Creates and caches loggers by category name.</summary>
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

/// <summary>Global log configuration.</summary>
public static class LogConfig
{
    /// <summary>Minimum severity to emit. Messages below this level are discarded.</summary>
    public static LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// When true, per-frame repetitive diagnostics (stage timing, render steps) are emitted at Trace level.
    /// When false (default), only one-time startup/lifecycle logs are shown — keeping the console clean at runtime.
    /// Enable via <c>LogConfig.PerFrameLogging = true</c> or the <c>ENGINE_LOG_FRAMES=1</c> environment variable.
    /// </summary>
    public static bool PerFrameLogging { get; set; }
        = Environment.GetEnvironmentVariable("ENGINE_LOG_FRAMES") == "1";

    /// <summary>Engine-wide stopwatch started at process launch for elapsed timestamps.</summary>
    internal static readonly Stopwatch EngineTimer = Stopwatch.StartNew();
}

/// <summary>Writes log messages to the console with elapsed time, level, and category. Flushes after every write for crash safety.</summary>
public sealed class ConsoleLoggerProvider : ILoggerProvider
{
    public static ConsoleLoggerProvider Instance { get; } = new();
    private ConsoleLoggerProvider() { }

    public void Log(LogLevel level, string category, string message, Exception? exception = null)
    {
        double elapsed = LogConfig.EngineTimer.Elapsed.TotalSeconds;
        var levelTag = level switch
        {
            LogLevel.Trace    => "TRACE",
            LogLevel.Debug    => "DEBUG",
            LogLevel.Info     => "INFO ",
            LogLevel.Warning  => "WARN ",
            LogLevel.Error    => "ERROR",
            LogLevel.Critical => "FATAL",
            _ => level.ToString().ToUpperInvariant()
        };
        Console.WriteLine($"[{elapsed,10:F4}s] [{levelTag}] [{category}] {message}");
        if (exception != null)
            Console.WriteLine(exception);
        Console.Out.Flush();
    }
}

/// <summary>Writes log messages to a file with auto-flush for crash safety. Initialized lazily on first use.</summary>
public sealed class FileLoggerProvider : ILoggerProvider, IDisposable
{
    private static FileLoggerProvider? _instance;
    public static FileLoggerProvider? Instance => _instance;

    private readonly StreamWriter _writer;
    private readonly object _lock = new();

    private FileLoggerProvider(StreamWriter writer) => _writer = writer;

    /// <summary>Initializes the file logger writing to the specified path. Safe to call multiple times; subsequent calls are ignored.</summary>
    public static void Initialize(string logFilePath)
    {
        if (_instance != null) return;
        try
        {
            var dir = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var stream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            _instance = new FileLoggerProvider(writer);
        }
        catch
        {
            // Silently degrade — file logging is best-effort.
        }
    }

    public void Log(LogLevel level, string category, string message, Exception? exception = null)
    {
        double elapsed = LogConfig.EngineTimer.Elapsed.TotalSeconds;
        var levelTag = level switch
        {
            LogLevel.Trace    => "TRACE",
            LogLevel.Debug    => "DEBUG",
            LogLevel.Info     => "INFO ",
            LogLevel.Warning  => "WARN ",
            LogLevel.Error    => "ERROR",
            LogLevel.Critical => "FATAL",
            _ => level.ToString().ToUpperInvariant()
        };
        lock (_lock)
        {
            _writer.WriteLine($"[{elapsed,10:F4}s] [{levelTag}] [{category}] {message}");
            if (exception != null)
                _writer.WriteLine(exception);
        }
    }

    public void Dispose()
    {
        lock (_lock) { _writer.Dispose(); }
    }
}

/// <summary>Static entry point for creating loggers.</summary>
public static class Log
{
    public static LoggerFactory Factory { get; } = new();
    public static ILogger Category(string category) => Factory.CreateLogger(category);
    public static ILogger For<T>() => Category(typeof(T).FullName ?? typeof(T).Name);

    /// <summary>Writes a startup banner with runtime, OS, and architecture information.</summary>
    public static void PrintStartupBanner()
    {
        var logger = Category("Engine");
        logger.Info("========================================================");
        logger.Info("  3DEngine — Initializing");
        logger.Info("========================================================");
        logger.Info($"Runtime:      {RuntimeInformation.FrameworkDescription}");
        logger.Info($"OS:           {RuntimeInformation.OSDescription}");
        logger.Info($"Architecture: {RuntimeInformation.ProcessArchitecture}");
        logger.Info($"Processors:   {Environment.ProcessorCount}");
        logger.Info($"Working Dir:  {Environment.CurrentDirectory}");
        logger.Info($"Base Dir:     {AppContext.BaseDirectory}");
        logger.Info($"Process ID:   {Environment.ProcessId}");
        logger.Info($"Timestamp:    {DateTime.UtcNow:O}");
        logger.Info($"Frame logs:   {(LogConfig.PerFrameLogging ? "ENABLED" : "DISABLED (set ENGINE_LOG_FRAMES=1 to enable)")}");
        logger.Info("========================================================");
    }
}
