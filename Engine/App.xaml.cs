﻿using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.IO;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor
{
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

            // Reset log
            if (File.Exists(logFilePath))
                File.WriteAllText(logFilePath, String.Empty);

            // Set up listener
            FileStream traceLog = new(logFilePath, FileMode.OpenOrCreate);
            TextWriterTraceListener listener = new(traceLog);

            // Pass listener to trace
            Trace.Listeners.Add(listener);
            // Automatically write into file
            Trace.AutoFlush = true;

            this.UnhandledException += (s, e) =>
            {
                e.Handled = true;

                // write date and time
                Debug.WriteLine($"[{DateTime.Now}]");

                // write call stack
                foreach (StackFrame stackFrame in new StackTrace().GetFrames())
                    Debug.Write(stackFrame.ToString());
                    //Debug.WriteLine($"{stackFrame.GetFileName()}.{stackFrame.GetMethod()} (Line) {stackFrame.GetFileLineNumber()}");

                // write exception
                Debug.WriteLine("\n" +e.Exception + "\n\n");

                Controller.Output.Log(e.Exception, Controller.EMessageType.Error);
            };
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            window = new MainWindow();
            window.Activate();
        }

        private Window window;
        public Window Window => window;
        //var window = (Application.Current as App)?.Window as MainWindow;
    }
}
