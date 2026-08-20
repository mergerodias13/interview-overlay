using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using InterviewOverlay.Hotkeys;
using Microsoft.Win32;

namespace InterviewOverlay.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            var s = App.Settings.Current;
            StartWithWindowsCheck.IsChecked = s.StartWithWindows;
            MinimizeToTrayCheck.IsChecked = s.MinimizeToTray;
            AlwaysOnTopCheck.IsChecked = s.AlwaysOnTop;
            ThemeCombo.SelectedIndex = s.Theme switch { "Light" => 1, "Custom" => 2, _ => 0 };

            HotkeyToggle.Text = s.Hotkeys.ToggleOverlay;
            HotkeyOpacityUp.Text = s.Hotkeys.OpacityUp;
            HotkeyOpacityDown.Text = s.Hotkeys.OpacityDown;
            HotkeyFontUp.Text = s.Hotkeys.FontUp;
            HotkeyFontDown.Text = s.Hotkeys.FontDown;
            HotkeyDetach.Text = s.Hotkeys.Detach;
        }

        private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox box) return;
            if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
                or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin or Key.System)
            {
                e.Handled = true;
                return;
            }

            uint mods = 0;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) mods |= 0x0002;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) mods |= 0x0004;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) mods |= 0x0001;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) mods |= 0x0008;

            if (mods == 0)
            {
                MessageBox.Show(this, "Global hotkeys need at least one modifier key (Ctrl, Shift, or Alt).",
                    "Interview Overlay", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
                return;
            }

            box.Text = HotkeyParser.Format(mods, e.Key);
            e.Handled = true;
        }

        private void ResetPosition_Click(object sender, RoutedEventArgs e)
        {
            App.Overlay?.ResetPosition();
            App.Overlay?.ShowOverlay();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var s = App.Settings.Current;
            s.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
            s.MinimizeToTray = MinimizeToTrayCheck.IsChecked == true;
            s.AlwaysOnTop = AlwaysOnTopCheck.IsChecked == true;
            s.Theme = (ThemeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Dark";

            s.Hotkeys.ToggleOverlay = HotkeyToggle.Text;
            s.Hotkeys.OpacityUp = HotkeyOpacityUp.Text;
            s.Hotkeys.OpacityDown = HotkeyOpacityDown.Text;
            s.Hotkeys.FontUp = HotkeyFontUp.Text;
            s.Hotkeys.FontDown = HotkeyFontDown.Text;
            s.Hotkeys.Detach = HotkeyDetach.Text;

            ApplyStartupSetting(s.StartWithWindows);

            App.Overlay?.SetAlwaysOnTop(s.AlwaysOnTop);
            App.Overlay?.ApplyTheme(s.Theme, s.CustomBackground, s.CustomText);

            App.Settings.Save();
            Close();
        }

        /// <summary>
        /// Adds/removes a Run-key entry for the current user only (no admin
        /// rights needed, no scheduled task, easy for the user to undo).
        /// </summary>
        private static void ApplyStartupSetting(bool enabled)
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            const string valueName = "InterviewOverlay";
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
                if (key == null) return;

                if (enabled)
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(exePath))
                        key.SetValue(valueName, $"\"{exePath}\"");
                }
                else
                {
                    if (key.GetValue(valueName) != null)
                        key.DeleteValue(valueName);
                }
            }
            catch
            {
                // Non-fatal - the user can still toggle "start with Windows" via
                // Windows Settings > Apps > Startup if this fails for any reason.
            }
        }
    }
}
