using System;
using System.Windows.Input;

namespace InterviewOverlay.Hotkeys
{
    public static class HotkeyParser
    {
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        public static bool TryParse(string combo, out uint modifiers, out uint vk)
        {
            modifiers = 0;
            vk = 0;
            if (string.IsNullOrWhiteSpace(combo)) return false;

            var parts = combo.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            string keyPart = parts[^1];

            foreach (var part in parts[..^1])
            {
                switch (part.ToLowerInvariant())
                {
                    case "ctrl":
                    case "control":
                        modifiers |= MOD_CONTROL;
                        break;
                    case "shift":
                        modifiers |= MOD_SHIFT;
                        break;
                    case "alt":
                        modifiers |= MOD_ALT;
                        break;
                    case "win":
                    case "windows":
                        modifiers |= MOD_WIN;
                        break;
                }
            }

            if (!Enum.TryParse<Key>(keyPart, ignoreCase: true, out var key))
                return false;

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            if (virtualKey == 0) return false;

            vk = (uint)virtualKey;
            return modifiers != 0; // require at least one modifier for a global hotkey
        }

        public static string Format(uint modifiers, Key key)
        {
            var parts = new System.Collections.Generic.List<string>();
            if ((modifiers & MOD_CONTROL) != 0) parts.Add("Ctrl");
            if ((modifiers & MOD_SHIFT) != 0) parts.Add("Shift");
            if ((modifiers & MOD_ALT) != 0) parts.Add("Alt");
            if ((modifiers & MOD_WIN) != 0) parts.Add("Win");
            parts.Add(key.ToString());
            return string.Join("+", parts);
        }
    }
}
