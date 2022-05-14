using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;
using YDKK.Windows;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.ApplicationModel.Resources.Core;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NSO_FriendStatus
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window m_window;
        private NotifyIcon NotifyIcon;
        public static DispatcherQueue DispatcherQueue { get; private set; }
        private const string ExitLabel = "終了";
        private const uint ExitCommandId = 0;
        private HMENU PopupMenu;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Get the app-level dispatcher
            DispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Register for toast activation. Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;

            // If we weren't launched by a toast, launch our window like normal.
            // Otherwise if launched by a toast, our OnActivated callback will be triggered
            if (!ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
            {
                LaunchAndBringToForegroundIfNeeded();
            }
        }

        private unsafe void InitializePopupMenu()
        {
            PopupMenu = PInvoke.CreatePopupMenu();
            var info = new MENUITEMINFOW
            {
                cbSize = (uint)Marshal.SizeOf<MENUITEMINFOW>(),
                fMask = MENU_ITEM_MASK.MIIM_ID | MENU_ITEM_MASK.MIIM_STRING,
                wID = ExitCommandId,
                cch = (uint)(ExitLabel.Length * sizeof(char)),
            };
            fixed (char* ptr = ExitLabel)
            {
                info.dwTypeData = ptr;
            }

            PInvoke.InsertMenuItem(PopupMenu, 0, true, &info);
        }

        private unsafe void LaunchAndBringToForegroundIfNeeded()
        {
            if (PopupMenu == IntPtr.Zero)
            {
                InitializePopupMenu();
            }
            if (m_window == null)
            {
                m_window = new MainWindow();

                m_window.Activated += (sender, e) =>
                {
                    switch (e.WindowActivationState)
                    {
                        case WindowActivationState.Deactivated:
                            //AppWindow?.Hide();
                            WindowHelper.HideWindow(m_window);
                            break;
                    }
                };

                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(m_window);
                var winId = Win32Interop.GetWindowIdFromWindow(hWnd);
                var appWindow = AppWindow.GetFromWindowId(winId);
                appWindow.Closing += (s, e) =>
                {
                    NotifyIcon?.Dispose();
                };
                //m_window.Activate();
                WindowHelper.SetWindowStyle(m_window);
                WindowHelper.SetWindowPosition(m_window);
                WindowHelper.ActivateWindow(m_window);
            }
            if (NotifyIcon == null)
            {
                //TODO
                var iconFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Icon.ico");
                var icon = Icon.FromFile(iconFile);
                NotifyIcon = new NotifyIcon("NSO-FriendStatus", icon);
                NotifyIcon.LButtonDoubleClick += (args) =>
                {
                    //m_window.Activate();
                    WindowHelper.SetWindowPosition(m_window);
                    WindowHelper.ActivateWindow(m_window);
                    WindowHelper.ShowWindow(m_window);
                };
                NotifyIcon.RButtonUp += (args) =>
                {
                    var hWnd = (HWND)NotifyIcon.WindowHandle;
                    PInvoke.SetForegroundWindow(hWnd);
                    PInvoke.TrackPopupMenuEx(PopupMenu, (uint)TRACK_POPUP_MENU_FLAGS.TPM_LEFTALIGN, args.xPos, args.yPos, hWnd);
                };
                NotifyIcon.MenuCommand += (args) =>
                {
                    switch (args)
                    {
                        case ExitCommandId:
                            {
                                NotifyIcon.Dispose();
                                m_window.Close();
                            }
                            break;
                    }
                };
            }

            // Additionally we show using our helper, since if activated via a toast, it doesn't
            // activate the window correctly
            WindowHelper.ShowWindow(m_window);
        }

        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            // Use the dispatcher from the window if present, otherwise the app dispatcher
            var dispatcherQueue = m_window?.DispatcherQueue ?? DispatcherQueue;

            dispatcherQueue.TryEnqueue(delegate
            {
                var args = ToastArguments.Parse(e.Argument);

                switch (args["action"])
                {
                    case "showUser":

                        // Launch/bring window to foreground
                        LaunchAndBringToForegroundIfNeeded();

                        // TODO: Open the user
                        break;
                }
            });
        }

        private static class WindowHelper
        {
            public static AppWindow GetAppWindow(Window window)
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var winId = Win32Interop.GetWindowIdFromWindow(hWnd);
                var appWindow = AppWindow.GetFromWindowId(winId);

                return appWindow;
            }
            public static void ShowWindow(Window window)
            {
                // Bring the window to the foreground... first get the window handle...
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

                // Restore window if minimized... requires DLL import above
                PInvoke.ShowWindow((HWND)hwnd, SHOW_WINDOW_CMD.SW_RESTORE);

                // And call SetForegroundWindow... requires DLL import above
                PInvoke.SetForegroundWindow((HWND)hwnd);
            }

            public static void ActivateWindow(Window window)
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var flag = ANIMATE_WINDOW_FLAGS.AW_ACTIVATE | ANIMATE_WINDOW_FLAGS.AW_BLEND | ANIMATE_WINDOW_FLAGS.AW_VER_NEGATIVE;
                PInvoke.AnimateWindow((HWND)hWnd, 200, flag);
            }

            public static void HideWindow(Window window)
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var flag = ANIMATE_WINDOW_FLAGS.AW_HIDE | ANIMATE_WINDOW_FLAGS.AW_BLEND | ANIMATE_WINDOW_FLAGS.AW_VER_POSITIVE;
                PInvoke.AnimateWindow((HWND)hWnd, 200, flag);
            }

            public static void SetWindowPosition(Window window)
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
                var displayArea = DisplayArea.GetFromWindowId(myWndId, DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    var newPosition = new RectInt32(0, 0, 350, 600)
                    {
                        X = displayArea.WorkArea.Width - 350,
                        Y = displayArea.WorkArea.Height - 600
                    };

                    var appWindow = GetAppWindow(window);
                    appWindow.MoveAndResize(newPosition, displayArea);
                }
            }

            public static void SetWindowStyle(Window window)
            {
                var appWindow = GetAppWindow(window);

                appWindow.TitleBar.BackgroundColor = Colors.Black;
                appWindow.TitleBar.ButtonBackgroundColor = Colors.Black;
                appWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
                appWindow.IsShownInSwitchers = false;
                var presenter = appWindow.Presenter as OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.IsAlwaysOnTop = true;
                    presenter.IsResizable = false;
                    presenter.IsMaximizable = false;
                    presenter.IsMinimizable = false;
                    presenter.SetBorderAndTitleBar(false, false);
                }
            }

        }
    }
}
