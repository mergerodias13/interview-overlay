using System;
using System.Collections.Generic;
using System.Windows.Interop;
using InterviewOverlay.Models;
using InterviewOverlay.WindowManagement;

namespace InterviewOverlay.Hotkeys
{
    /// <summary>
    /// Registers OS-level (global) hotkeys using RegisterHotKey, so they
    /// fire even while Zoom/Teams/Chrome has focus. Uses a hidden message-only
    /// window to receive WM_HOTKEY messages.
    /// </summary>
    public class GlobalHotkeyManager : IDisposable
    {
        private HwndSource? _source;
        private readonly Dictionary<int, Action> _actions = new();
        private int _nextId = 1;

        public event EventHandler? ToggleOverlay;
        public event EventHandler? OpacityUp;
        public event EventHandler? OpacityDown;
        public event EventHandler? FontUp;
        public event EventHandler? FontDown;
        public event EventHandler? Detach;

        public GlobalHotkeyManager()
        {
            var parameters = new HwndSourceParameters("InterviewOverlayHotkeySink")
            {
                Width = 0,
                Height = 0,
                WindowStyle = 0
            };
            _source = new HwndSource(parameters);
            _source.AddHook(WndProc);
        }

        public void RegisterDefaults(HotkeyBindings bindings)
        {
            TryRegister(bindings.ToggleOverlay, () => ToggleOverlay?.Invoke(this, EventArgs.Empty));
            TryRegister(bindings.OpacityUp, () => OpacityUp?.Invoke(this, EventArgs.Empty));
            TryRegister(bindings.OpacityDown, () => OpacityDown?.Invoke(this, EventArgs.Empty));
            TryRegister(bindings.FontUp, () => FontUp?.Invoke(this, EventArgs.Empty));
            TryRegister(bindings.FontDown, () => FontDown?.Invoke(this, EventArgs.Empty));
            TryRegister(bindings.Detach, () => Detach?.Invoke(this, EventArgs.Empty));
        }

        /// <summary>Parses strings like "Ctrl+Shift+H" and registers them.</summary>
        public bool TryRegister(string combo, Action onPressed)
        {
            if (!HotkeyParser.TryParse(combo, out uint modifiers, out uint vk)) return false;

            int id = _nextId++;
            bool ok = NativeMethodsInterop.RegisterHotKey(_source!.Handle, id, modifiers, vk);
            if (ok) _actions[id] = onPressed;
            return ok;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_actions.TryGetValue(id, out var action))
                {
                    action.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_source != null)
            {
                foreach (var id in _actions.Keys)
                    NativeMethodsInterop.UnregisterHotKey(_source.Handle, id);
                _source.RemoveHook(WndProc);
                _source.Dispose();
                _source = null;
            }
        }
    }

    // Thin wrapper so Hotkeys doesn't need a direct reference to WindowManagement's
    // internal NativeMethods class (kept internal there on purpose).
    internal static class NativeMethodsInterop
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
