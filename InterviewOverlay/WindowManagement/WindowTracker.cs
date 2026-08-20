using System;
using System.Windows.Threading;

namespace InterviewOverlay.WindowManagement
{
    public enum TargetWindowState { Normal, Minimized, Closed }

    public class WindowTracker
    {
        private readonly DispatcherTimer _timer;
        private RECT _lastRect;
        private bool _lastMinimized;
        private bool _hasTarget;

        public IntPtr TargetHandle { get; private set; }

        public event EventHandler<RECT>? RectChanged;
        public event EventHandler? Minimized;
        public event EventHandler? Restored;
        public event EventHandler? TargetClosed;

        public WindowTracker()
        {
            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                // Poll frequently enough to feel "live" without wasting CPU.
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _timer.Tick += (_, _) => Poll();
        }

        public void Attach(IntPtr handle)
        {
            TargetHandle = handle;
            _hasTarget = true;
            if (WindowEnumerator.TryGetRect(handle, out var rect))
                _lastRect = rect;
            _lastMinimized = WindowEnumerator.IsMinimized(handle);
            _timer.Start();
        }

        public void Detach()
        {
            _hasTarget = false;
            TargetHandle = IntPtr.Zero;
            _timer.Stop();
        }

        private void Poll()
        {
            if (!_hasTarget) return;

            if (!WindowEnumerator.StillExists(TargetHandle))
            {
                Detach();
                TargetClosed?.Invoke(this, EventArgs.Empty);
                return;
            }

            bool minimizedNow = WindowEnumerator.IsMinimized(TargetHandle);
            if (minimizedNow != _lastMinimized)
            {
                _lastMinimized = minimizedNow;
                if (minimizedNow) Minimized?.Invoke(this, EventArgs.Empty);
                else Restored?.Invoke(this, EventArgs.Empty);
            }

            if (minimizedNow) return; // no meaningful rect while minimized

            if (WindowEnumerator.TryGetRect(TargetHandle, out var rect))
            {
                if (rect.Left != _lastRect.Left || rect.Top != _lastRect.Top ||
                    rect.Right != _lastRect.Right || rect.Bottom != _lastRect.Bottom)
                {
                    _lastRect = rect;
                    RectChanged?.Invoke(this, rect);
                }
            }
        }
    }
}
