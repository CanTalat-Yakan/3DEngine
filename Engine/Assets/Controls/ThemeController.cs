using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using WinRT;
using System.Runtime.InteropServices; // For DllImport
using WinRT; // required to support Window.As<ICompositionSupportsSystemBackdrop>()
using System.Runtime.InteropServices; // For DllImport
using Microsoft.UI.Composition.SystemBackdrops;
using Frame = Microsoft.UI.Xaml.Controls.Frame;
using System.Drawing;
using Microsoft.UI.Xaml.Media;

namespace Editor.Controls
{
    internal class ThemeController
    {
        private WindowsSystemDispatcherQueueHelper _wsdqHelper; // See separate sample below for implementation
        private MicaController _micaController;
        private SystemBackdropConfiguration _configurationSource;
        private Frame _frame;
        private MainWindow _window;

        public ThemeController(Frame frame, MainWindow window)
        {
            _frame = frame;
            _window = window;

            Initialize();
        }

        private bool Initialize()
        {
            if (MicaController.IsSupported())
            {
                _wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                _wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // Hooking up the policy object
                _configurationSource = new SystemBackdropConfiguration();

                _window.Activated += Window_Activated;
                _window.Closed += Window_Closed;
                //_frame.ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                _configurationSource.IsInputActive = true;

                _micaController = new MicaController();

                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                _micaController.AddSystemBackdropTarget(_window.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                _micaController.SetSystemBackdropConfiguration(_configurationSource);
                return true; // succeeded

            }

            return false; // Mica is not supported on this system
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

            _window.Activated -= Window_Activated;
            _configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (_configurationSource != null)
                SetRequstedTheme();
        }

        public void SetRequstedTheme()
        {
            _frame.RequestedTheme = _frame.RequestedTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
            _configurationSource.Theme = _configurationSource.Theme == SystemBackdropTheme.Light ? SystemBackdropTheme.Dark : SystemBackdropTheme.Light;

            MainController.Instance.LayoutControl.Output.ChangeColorWithTheme.Background =
                _frame.RequestedTheme == ElementTheme.Light
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 40, 40));

            MainController.Instance.LayoutControl.Files.ChangeColorWithTheme.Background =
                _frame.RequestedTheme == ElementTheme.Light
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 40, 40));
        }
    }

    public class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }
    }
}
