using System.Runtime.InteropServices;
using WinRT;

using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;

namespace Editor.Controller;

internal sealed class Theme
{
    public static Theme Instance { get; private set; }

    private Page _page;
    private MainWindow _mainWindow;

    private WindowsSystemDispatcherQueueHelper _wsdqHelper;
    private MicaController _micaController;
    private SystemBackdropConfiguration _configurationSource;

    public Theme(MainWindow mainWindow, Page page)
    {
        // Initializes the singleton instance of the class, if it hasn't been already.
        if (Instance is null)
            Instance = this;

        // Assign local variables.
        _mainWindow = mainWindow;
        _page = page;


        if (MicaController.IsSupported())
        {
            _wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            _wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            // Hooking up the policy object
            _configurationSource = new SystemBackdropConfiguration();

            _mainWindow.Activated += Window_Activated;
            _mainWindow.Closed += Window_Closed;
            //_frame.ActualThemeChanged += Window_ThemeChanged;

            // Initial configuration state.
            _configurationSource.IsInputActive = true;

            _micaController = new MicaController();

            // Enable the system backdrop.
            // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
            _micaController.AddSystemBackdropTarget(_mainWindow.As<ICompositionSupportsSystemBackdrop>());
            _micaController.SetSystemBackdropConfiguration(_configurationSource);
        }
    }

    public void SetRequestedTheme(ElementTheme? requestedTheme = null)
    {
        // Check if requested theme is not null.
        if (requestedTheme is null)
        {
            // If it's null, set the page theme to opposite of its current theme.
            _page.RequestedTheme = _page.RequestedTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
            // Also set the configuration source theme to opposite of its current theme.
            _configurationSource.Theme = _configurationSource.Theme == SystemBackdropTheme.Light ? SystemBackdropTheme.Dark : SystemBackdropTheme.Light;
        }
        else
        {
            // If it's not null, set the page theme to the requested theme.
            _page.RequestedTheme = requestedTheme.Value;

            // Based on the requested theme, set the configuration source theme.
            if (requestedTheme.Value == ElementTheme.Light)
                _configurationSource.Theme = SystemBackdropTheme.Light;
            if (requestedTheme.Value == ElementTheme.Dark)
                _configurationSource.Theme = SystemBackdropTheme.Dark;
            if (requestedTheme.Value == ElementTheme.Default)
                _configurationSource.Theme = SystemBackdropTheme.Default;
        }

        // Check if the Main instance is not null.
        if (Main.Instance is not null)
        {
            // If it's not null, set the background color of tabs root and change the color with theme of output and files.
            Main.Instance.LayoutControl.TabsRoot.Background =
                _page.RequestedTheme == ElementTheme.Light
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 243, 243, 243))
                    : Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] as SolidColorBrush;

            Main.Instance.LayoutControl.Output.ChangeColorWithTheme.Background =
                _page.RequestedTheme == ElementTheme.Light
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 249, 249, 249))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 40, 40));

            Main.Instance.LayoutControl.Files.ChangeColorWithTheme.Background =
                _page.RequestedTheme == ElementTheme.Light
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 249, 249, 249))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 40, 40));
        }

        // Check if the Engine.Core instance is not null.
        if (Engine.Core.Instance is not null)
            // If it's not null, set the theme for entity manager.
            Engine.SceneSystem.SceneManager.MainScene.EntityManager?
                .GetFromTag("DefaultSky")?
                .GetComponent<Engine.Editor.DefaultSky>()?
                .SetTheme(_page.RequestedTheme == ElementTheme.Light);
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args) =>
        // Set the IsInputActive property of the ConfigurationSource to the opposite of the WindowActivationState.
        _configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
        // use this closed window.
        if (_micaController is not null)
        {
            _micaController.Dispose();
            _micaController = null;
        }

        _mainWindow.Activated -= Window_Activated;
        _configurationSource = null;
    }
}

internal sealed class WindowsSystemDispatcherQueueHelper
{
    [StructLayout(LayoutKind.Sequential)]
    struct DispatcherQueueOptions
    {
        internal int _dwSize;
        internal int _threadType;
        internal int _apartmentType;
    }

    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController(
        [In] DispatcherQueueOptions options,
        [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

    private object _dispatcherQueueController = null;

    public void EnsureWindowsSystemDispatcherQueueController()
    {
        if (Windows.System.DispatcherQueue.GetForCurrentThread() is not null)
            // One already exists, so we'll just use it.
            return;

        if (_dispatcherQueueController == null)
        {
            DispatcherQueueOptions options;
            options._dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
            options._threadType = 2;    // DQTYPE_THREAD_CURRENT
            options._apartmentType = 2; // DQTAT_COM_STA

            CreateDispatcherQueueController(options, ref _dispatcherQueueController);
        }
    }
}
