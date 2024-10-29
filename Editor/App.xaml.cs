using System.IO;
using System;

using Microsoft.UI.Xaml;

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

        Engine.Helpers.ExceptionHandler.CreateTraceLog(rootPath, logFilePath);

        this.UnhandledException += (s, e) =>
        {
            if (Main.Instance is not null)
                Engine.Helpers.ExceptionHandler.HandleException(e.Exception);

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
    public Window Window => window;
    // USE CASE: var window = (Application.Current as App)?.Window as MainWindow;
    // USE CASE: var hWnd = (Application.Current as App)?.Window.GetWindowHandle();
    // var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as App)?.Window as MainWindow);

}
