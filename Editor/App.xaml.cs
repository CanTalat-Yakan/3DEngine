using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System;
using Editor.Controller;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

//[assembly: PostSharp.Patterns.Model.WeakEvent(AttributeTargetTypes = "Engine.*")]
//[assembly: PostSharp.Patterns.Model.WeakEvent]
namespace Editor;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        var documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var rootPath = Path.Combine(documentsDir, "3DEngine");
        var logFilePath = Path.Combine(rootPath, "Application.log");

        // Create directory.
        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);

        // Increment log if it is locked by another process.
        bool? isLocked = logFilePath.IsFileLocked();
        if (isLocked is not null)
            if (isLocked.Value)
                logFilePath = logFilePath.IncrementPathIfExists(
                    Directory.GetFiles(rootPath)
                        .Select(p => Path.GetFileNameWithoutExtension(p))
                        .ToArray());

        // Reset log.
        if (File.Exists(logFilePath))
            File.WriteAllText(logFilePath, String.Empty);

        // Set up listener.
        FileStream traceLog = new(logFilePath, FileMode.OpenOrCreate);
        TextWriterTraceListener listener = new(traceLog);

        // Pass listener to trace.
        Trace.Listeners.Add(listener);
        // Automatically write into file.
        Trace.AutoFlush = true;

        this.UnhandledException += (s, e) =>
        {
            // Write date and time.
            Debug.WriteLine($"[{DateTime.Now}]");

            // Write file name, line number, and method name.
            StackTrace stackTrace = new StackTrace(e.Exception, true);
            StackFrame frame = stackTrace.GetFrame(0); // Get the top frame (most recent method call).

            string fileName = frame.GetFileName();
            int lineNumber = frame.GetFileLineNumber();
            string methodName = frame.GetMethod().Name;

            if (fileName is not null)
                Debug.WriteLine($"{fileName}:{lineNumber} ({methodName})");

            Debug.WriteLine(e.Exception);

            if (Main.Instance is not null)
                Output.Log(e.Exception, MessageType.Error, lineNumber, methodName, fileName);

            // Mark the event as handled to prevent it from being processed further.
            e.Handled = true;
        };
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        window = new MainWindow();
        window.Activate();
    }

    private Window window;
    public Window Window => window; //USE CASE: var window = (Microsoft.UI.Xaml.Application.Current as App)?.Window as MainWindow;
}
