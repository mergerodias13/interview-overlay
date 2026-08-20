using System.Collections.Generic;

namespace InterviewOverlay.Models
{
    public class HotkeyBindings
    {
        // Stored as strings like "Ctrl+Shift+H" and parsed by GlobalHotkeyManager.
        public string ToggleOverlay { get; set; } = "Ctrl+Shift+H";
        public string OpacityUp { get; set; } = "Ctrl+Shift+Up";
        public string OpacityDown { get; set; } = "Ctrl+Shift+Down";
        public string FontUp { get; set; } = "Ctrl+Shift+Oem Plus";
        public string FontDown { get; set; } = "Ctrl+Shift+OemMinus";
        public string Detach { get; set; } = "Ctrl+Shift+D";
    }

    public class AppSettings
    {
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public int AutoSaveIntervalSeconds { get; set; } = 5;

        public bool AlwaysOnTop { get; set; } = true;
        public bool ClickThrough { get; set; } = false;

        public string Theme { get; set; } = "Dark"; // Dark / Light / Custom
        public string CustomBackground { get; set; } = "#1E1F24";
        public string CustomText { get; set; } = "#F0F0F0";
        public string CustomAccent { get; set; } = "#4C8BF5";

        public HotkeyBindings Hotkeys { get; set; } = new();

        public double LastOverlayX { get; set; } = double.NaN;
        public double LastOverlayY { get; set; } = double.NaN;
    }
}
