using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Engine;

/// <summary>Severity levels for log messages, ordered from least to most severe.</summary>
public enum LogLevel
{
    /// <summary>Finest-grained diagnostic detail (variable dumps, method entry/exit).</summary>
    Trace,
    /// <summary>Internal development diagnostics not shown to end users.</summary>
    Debug,
    /// <summary>Informational messages about normal application flow.</summary>
    Info,
    /// <summary>Potentially harmful situations that do not prevent operation.</summary>
    Warning,
    /// <summary>Recoverable failures that may degrade functionality.</summary>
    Error,
    /// <summary>Unrecoverable failures that will terminate the application.</summary>
    Critical
}

/// <summary>Sink that receives formatted log messages from the logging infrastructure.</summary>
public interface ILoggerProvider
{
    /// <summary>Writes a log entry.</summary>
    /// <param name="level">The severity of the message.</param>
    /// <param name="category">The logger category (e.g., <c>"Engine.World"</c>).</param>
    /// <param name="message">The log message text.</param>
    /// <param name="exception">Optional exception associated with the message.</param>
    void Log(LogLevel level, string category, string message, Exception? exception = null);
}

/// <summary>Category-scoped logging interface providing convenience methods for each severity level.</summary>
public interface ILogger
{
    /// <summary>Writes a log entry at the specified severity level.</summary>
    /// <param name="level">The severity of the message.</param>
    /// <param name="message">The log message text.</param>
    /// <param name="exception">Optional exception associated with the message.</param>
    void Log(LogLevel level, string message, Exception? exception = null);

    /// <summary>Writes a trace-level message for detailed diagnostic information.</summary>
    /// <param name="message">The log message text.</param>
    void Trace(string message);

    /// <summary>Trace-level log that only emits when <see cref="LogConfig.PerFrameLogging"/> is enabled. Use for per-frame repetitive diagnostics (stage timing, render steps, etc.).</summary>
    /// <param name="message">The log message text.</param>
    void FrameTrace(string message);

    /// <summary>Writes a debug-level message for internal development diagnostics.</summary>
    /// <param name="message">The log message text.</param>
    void Debug(string message);

    /// <summary>Writes an informational message about normal application flow.</summary>
    /// <param name="message">The log message text.</param>
    void Info(string message);

    /// <summary>Writes a warning about a potentially harmful situation.</summary>
    /// <param name="message">The log message text.</param>
    void Warn(string message);

    /// <summary>Writes an error message about a recoverable failure.</summary>
    /// <param name="message">The log message text.</param>
    /// <param name="exception">Optional exception that caused the error.</param>
    void Error(string message, Exception? exception = null);

    /// <summary>Writes a critical error message about a failure that may terminate the application.</summary>
    /// <param name="message">The log message text.</param>
    /// <param name="exception">Optional exception that caused the critical failure.</param>
    void Critical(string message, Exception? exception = null);
}

/// <summary>
/// Default logger implementation that dispatches to the console provider, file provider,
/// and any additional user-added providers.
/// </summary>
/// <seealso cref="ConsoleLoggerProvider"/>
/// <seealso cref="FileLoggerProvider"/>
/// <seealso cref="LogConfig"/>
public sealed class Logger : ILogger
{
    private readonly string _category;
    private readonly List<ILoggerProvider> _extraProviders = new();

    /// <summary>Creates a new logger for the specified category.</summary>
    /// <param name="category">A hierarchical category name, e.g. <c>"Engine.World"</c>.</param>
    public Logger(string category)
    {
        _category = category;
    }

    /// <summary>
    /// Adds an extra log provider to this logger. Console and file providers are always included
    /// and cannot be added via this method.
    /// </summary>
    /// <param name="provider">The provider to add.</param>
    /// <returns>This <see cref="Logger"/> instance for fluent chaining.</returns>
    public Logger UseProvider(ILoggerProvider provider)
    {
        if (!_extraProviders.Contains(provider)
            && provider != ConsoleLoggerProvider.Instance
            && provider is not FileLoggerProvider)
            _extraProviders.Add(provider);
        return this;
    }

    /// <inheritdoc />
    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        // Console: respects ConsoleMinimumLevel (defaults to Info - no startup traces on console).
        if (level >= LogConfig.ConsoleMinimumLevel)
            ConsoleLoggerProvider.Instance.Log(level, _category, message, exception);

        // File: respects MinimumLevel (defaults to Trace - all startup diagnostics captured to disk).
        // Late-bound lookup so loggers created before Initialize() still write to the file.
        if (level >= LogConfig.MinimumLevel && FileLoggerProvider.Instance is { } file)
            file.Log(level, _category, message, exception);

        // Any extra user-added providers.
        foreach (var provider in _extraProviders)
            if (level >= LogConfig.MinimumLevel)
                provider.Log(level, _category, message, exception);
    }

    /// <inheritdoc />
    public void Trace(string message) => Log(LogLevel.Trace, message);
    /// <inheritdoc />
    public void FrameTrace(string message) { if (LogConfig.PerFrameLogging) Log(LogLevel.Trace, message); }
    /// <inheritdoc />
    public void Debug(string message) => Log(LogLevel.Debug, message);
    /// <inheritdoc />
    public void Info(string message) => Log(LogLevel.Info, message);
    /// <inheritdoc />
    public void Warn(string message) => Log(LogLevel.Warning, message);
    /// <inheritdoc />
    public void Error(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);
    /// <inheritdoc />
    public void Critical(string message, Exception? exception = null) => Log(LogLevel.Critical, message, exception);
}

/// <summary>Creates and caches <see cref="Logger"/> instances by category name.</summary>
public sealed class LoggerFactory
{
    private readonly Dictionary<string, Logger> _loggers = new();

    /// <summary>Creates or retrieves a cached logger for the given category.</summary>
    /// <param name="category">The category name for the logger.</param>
    /// <returns>A <see cref="Logger"/> instance for the specified category.</returns>
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
    /// <summary>Minimum severity written to the log file and any extra providers. Defaults to Trace - captures all startup diagnostics to disk.</summary>
    public static LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <summary>Minimum severity written to the console. Defaults to Info - keeps the console readable while the log file gets the full detail.</summary>
    public static LogLevel ConsoleMinimumLevel { get; set; } = LogLevel.Info;

    /// <summary>
    /// When true, per-frame repetitive diagnostics (stage timing, render steps) are emitted at Trace level.
    /// When false (default), only one-time startup/lifecycle logs are shown - keeping output clean at runtime.
    /// Enable via <c>LogConfig.PerFrameLogging = true</c> or the <c>ENGINE_LOG_FRAMES=1</c> environment variable.
    /// </summary>
    public static bool PerFrameLogging { get; set; }
        = Environment.GetEnvironmentVariable("ENGINE_LOG_FRAMES") == "1";

    /// <summary>Maximum log file size in bytes. When exceeded the file logger writes a truncation notice and stops. Defaults to 50 MB.</summary>
    public static long MaxLogFileBytes { get; set; } = 50L * 1024 * 1024;

    /// <summary>Engine-wide stopwatch started at process launch for elapsed timestamps.</summary>
    internal static readonly Stopwatch EngineTimer = Stopwatch.StartNew();
}

/// <summary>Writes log messages to the console with elapsed time, level, and category. Flushes after every write for crash safety.</summary>
/// <seealso cref="FileLoggerProvider"/>
/// <seealso cref="LogConfig"/>
public sealed class ConsoleLoggerProvider : ILoggerProvider
{
    /// <summary>Singleton instance of the console log provider.</summary>
    public static ConsoleLoggerProvider Instance { get; } = new();
    private ConsoleLoggerProvider() { }

    /// <inheritdoc />
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

/// <summary>
/// Writes log messages to a file with auto-flush for crash safety. Initialized lazily on first use.
/// Truncates output when the file exceeds <see cref="LogConfig.MaxLogFileBytes"/>.
/// </summary>
/// <seealso cref="ConsoleLoggerProvider"/>
/// <seealso cref="LogConfig"/>
public sealed class FileLoggerProvider : ILoggerProvider, IDisposable
{
    private static FileLoggerProvider? _instance;

    /// <summary>The active file logger instance, or <c>null</c> if not yet initialized.</summary>
    public static FileLoggerProvider? Instance => _instance;

    private readonly StreamWriter _writer;
    private readonly object _lock = new();
    private long _bytesWritten;
    private bool _truncated;

    private FileLoggerProvider(StreamWriter writer) => _writer = writer;

    /// <summary>
    /// Initializes the file logger writing to the specified path.
    /// Safe to call multiple times; subsequent calls are ignored.
    /// </summary>
    /// <param name="logFilePath">The absolute path of the log file to create or overwrite.</param>
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
            // Silently degrade - file logging is best-effort.
        }
    }

    /// <inheritdoc />
    public void Log(LogLevel level, string category, string message, Exception? exception = null)
    {
        lock (_lock)
        {
            if (_truncated) return;

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

            var line = $"[{elapsed,10:F4}s] [{levelTag}] [{category}] {message}";
            _writer.WriteLine(line);
            _bytesWritten += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;

            if (exception != null)
            {
                var exStr = exception.ToString();
                _writer.WriteLine(exStr);
                _bytesWritten += Encoding.UTF8.GetByteCount(exStr) + Environment.NewLine.Length;
            }

            if (_bytesWritten >= LogConfig.MaxLogFileBytes)
            {
                _writer.WriteLine("--- LOG TRUNCATED (size limit reached) ---");
                _truncated = true;
            }
        }
    }

    /// <summary>Flushes and closes the underlying file stream.</summary>
    public void Dispose()
    {
        lock (_lock) { _writer.Dispose(); }
    }
}

/// <summary>
/// Static entry point for creating category-scoped loggers.
/// </summary>
/// <seealso cref="Logger"/>
/// <seealso cref="LoggerFactory"/>
public static class Log
{
    /// <summary>Shared logger factory that caches instances by category.</summary>
    public static LoggerFactory Factory { get; } = new();

    /// <summary>Creates or retrieves a logger for the specified category name.</summary>
    /// <param name="category">The category name, e.g. <c>"Engine.Schedule"</c>.</param>
    /// <returns>A cached <see cref="ILogger"/> for the category.</returns>
    public static ILogger Category(string category) => Factory.CreateLogger(category);

    /// <summary>Creates or retrieves a logger using the full name of <typeparamref name="T"/> as category.</summary>
    /// <typeparam name="T">The type whose full name becomes the logger category.</typeparam>
    /// <returns>A cached <see cref="ILogger"/> for the type.</returns>
    public static ILogger For<T>() => Category(typeof(T).FullName ?? typeof(T).Name);

    /// <summary>Writes a startup banner with runtime, OS, and architecture information.</summary>
    public static void PrintStartupBanner()
    {
        var logger = Category("Engine");
        logger.Info("========================================================");
        logger.Info("  3DEngine - Initializing");
        logger.Info("========================================================");
        logger.Info($"Runtime:      {RuntimeInformation.FrameworkDescription}");
        logger.Info($"OS:           {RuntimeInformation.OSDescription}");
        logger.Info($"Architecture: {RuntimeInformation.ProcessArchitecture}");
        logger.Info($"Processors:   {Environment.ProcessorCount}");
        logger.Info($"Working Dir:  {Environment.CurrentDirectory}");
        logger.Info($"Base Dir:     {AppContext.BaseDirectory}");
        logger.Info($"Process ID:   {Environment.ProcessId}");
        logger.Info($"Timestamp:    {DateTime.UtcNow:O}");
        logger.Info($"Console log:  {LogConfig.ConsoleMinimumLevel}+");
        logger.Info($"File log:     {LogConfig.MinimumLevel}+ → {Path.Combine(AppContext.BaseDirectory, "Engine.log")}");
        logger.Info($"File cap:     {LogConfig.MaxLogFileBytes / (1024 * 1024)} MB");
        logger.Info($"Frame logs:   {(LogConfig.PerFrameLogging ? "ENABLED" : "DISABLED (set ENGINE_LOG_FRAMES=1 to enable)")}");
        logger.Info("========================================================");
    }
}
