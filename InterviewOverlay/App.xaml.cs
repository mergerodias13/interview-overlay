using System;
using System.Threading;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using InterviewOverlay.Hotkeys;
using InterviewOverlay.Services;

namespace InterviewOverlay
{
    public partial class App : Application
    {
        private static Mutex? _singleInstanceMutex;
        public static TaskbarIcon? TrayIcon { get; private set; }
        public static GlobalHotkeyManager? Hotkeys { get; private set; }
        public static SettingsManager Settings { get; private set; } = new();
        public static NoteRepository Notes { get; private set; } = new();

        public static MainWindow? Main { get; set; }
        public static OverlayWindow? Overlay { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool createdNew;
            _singleInstanceMutex = new Mutex(true, "InterviewOverlay_SingleInstance_Mutex", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("Interview Overlay is already running. Check your system tray.",
                    "Interview Overlay", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            Settings.Load();
            Notes.Load();

            TrayIcon = (TaskbarIcon)FindResource("AppTrayIcon");
            // Uses a built-in system icon so the app runs without a bundled
            // .ico file. Swap in Resources/tray.ico and set TrayIcon.Icon
            // there instead if you want custom branding.
            TrayIcon.Icon = System.Drawing.SystemIcons.Application;

            Main = new MainWindow();
            Overlay = new OverlayWindow();

            Hotkeys = new GlobalHotkeyManager();
            Hotkeys.RegisterDefaults(Settings.Current.Hotkeys);
            Hotkeys.ToggleOverlay += (_, _) => Overlay?.ToggleVisibility();
            Hotkeys.OpacityUp += (_, _) => Overlay?.AdjustOpacity(0.05);
            Hotkeys.OpacityDown += (_, _) => Overlay?.AdjustOpacity(-0.05);
            Hotkeys.FontUp += (_, _) => Overlay?.AdjustFontSize(1);
            Hotkeys.FontDown += (_, _) => Overlay?.AdjustFontSize(-1);
            Hotkeys.Detach += (_, _) => Overlay?.Detach();

            Main.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Hotkeys?.Dispose();
            Notes.Save();
            Settings.Save();
            TrayIcon?.Dispose();
            _singleInstanceMutex?.ReleaseMutex();
            base.OnExit(e);
        }

        public static void ShutdownApp()
        {
            Current.Shutdown();
        }

        private void TrayShowOverlay_Click(object sender, RoutedEventArgs e) => Overlay?.ShowOverlay();
        private void TrayHideOverlay_Click(object sender, RoutedEventArgs e) => Overlay?.HideOverlay();
        private void TrayOpenNotes_Click(object sender, RoutedEventArgs e) { Main?.Show(); Main?.Activate(); }
        private void TrayAttach_Click(object sender, RoutedEventArgs e) { Main?.Show(); Main?.OpenAttachDialog(); }
        private void TrayDetach_Click(object sender, RoutedEventArgs e) => Overlay?.Detach();
        private void TraySettings_Click(object sender, RoutedEventArgs e) => Main?.OpenSettings();
        private void TrayExit_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.AppIsExiting = true;
            ShutdownApp();
        }
    }
}
