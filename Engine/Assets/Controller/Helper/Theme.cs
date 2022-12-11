using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using WinRT;
using Engine.Utilities;

namespace Editor.Controller
{
    internal class Theme
    {
        private WindowsSystemDispatcherQueueHelper _wsdqHelper;
        private MicaController _micaController;
        private SystemBackdropConfiguration _configurationSource;
        private Page _page;
        private MainWindow _mainWindow;

        public Theme(MainWindow mainWindow, Page page)
        {
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
                _micaController.AddSystemBackdropTarget(_mainWindow.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                _micaController.SetSystemBackdropConfiguration(_configurationSource);
            }
        }

        public void SetRequstedTheme()
        {
            _page.RequestedTheme = _page.RequestedTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
            _configurationSource.Theme = _configurationSource.Theme == SystemBackdropTheme.Light ? SystemBackdropTheme.Dark : SystemBackdropTheme.Light;

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

            if (Engine.Core.Instance != null)
                SceneManager.Scene.EntitytManager.SetTheme(_page.RequestedTheme == ElementTheme.Light);
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            _configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed window.
            if (_micaController != null)
            {
                _micaController.Dispose();
                _micaController = null;
            }

            _mainWindow.Activated -= Window_Activated;
            _configurationSource = null;
        }
    }

    public class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int _dwSize;
            internal int _threadType;
            internal int _apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        private object _dispatcherQueueController = null;

        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
                // one already exists, so we'll just use it.
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
}
