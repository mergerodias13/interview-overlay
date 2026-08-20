using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using InterviewOverlay.Models;
using InterviewOverlay.WindowManagement;

namespace InterviewOverlay
{
    /// <summary>
    /// The floating notes panel. Openly visible to the user; nothing here
    /// tries to hide the notes from screen capture. "Attach to Window"
    /// keeps it positioned relative to a chosen target window (e.g. your
    /// Zoom or Meet window) so it moves along with it.
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private readonly WindowTracker _tracker = new();
        private IntPtr _attachedHandle = IntPtr.Zero;
        private string _attachedPosition = "TopRight";
        private bool _isDraggingSelf;
        private Point _dragStart;

        public InterviewProfile? CurrentProfile { get; private set; }
        public bool IsClickThrough { get; private set; }
        public string? AttachedWindowLabel { get; private set; }

        public event EventHandler? AttachmentChanged;

        public OverlayWindow()
        {
            InitializeComponent();

            var settings = App.Settings.Current;
            Topmost = settings.AlwaysOnTop;
            ApplyTheme(settings.Theme, settings.CustomBackground, settings.CustomText);

            if (!double.IsNaN(settings.LastOverlayX) && !double.IsNaN(settings.LastOverlayY))
            {
                Left = settings.LastOverlayX;
                Top = settings.LastOverlayY;
            }
            else
            {
                PositionAtDefault();
            }

            _tracker.RectChanged += (_, rect) => Dispatcher.Invoke(() => RepositionRelativeToTarget(rect));
            _tracker.Minimized += (_, _) => Dispatcher.Invoke(() => Hide());
            _tracker.Restored += (_, _) => Dispatcher.Invoke(() => { if (Visibility != Visibility.Visible) ShowOverlay(); });
            _tracker.TargetClosed += (_, _) => Dispatcher.Invoke(Detach);

            LoadProfile(App.Notes.GetLastOpened());
            ShowOverlay();
        }

        // ---------- Profile / content ----------

        public void LoadProfile(InterviewProfile? profile)
        {
            if (profile == null) return;
            CurrentProfile = profile;
            NotesText.Text = profile.NotesPlainText;
            Width = profile.OverlayWidth;
            Height = profile.OverlayHeight;
            SetOpacity(profile.OverlayOpacity);
            NotesText.FontSize = profile.FontSize;
            _attachedPosition = profile.OverlayPosition;
        }

        public void RefreshNotesText(string text) => NotesText.Text = text;

        public void SaveViewStateToProfile()
        {
            if (CurrentProfile == null) return;
            CurrentProfile.OverlayWidth = Width;
            CurrentProfile.OverlayHeight = Height;
            CurrentProfile.OverlayOpacity = Opacity;
            CurrentProfile.FontSize = NotesText.FontSize;
            CurrentProfile.OverlayPosition = _attachedPosition;
        }

        // ---------- Visibility ----------

        public void ShowOverlay()
        {
            Show();
            Activate();
        }

        public void HideOverlay() => Hide();

        public void ToggleVisibility()
        {
            if (Visibility == Visibility.Visible) HideOverlay();
            else ShowOverlay();
        }

        // ---------- Opacity / font ----------

        public void SetOpacity(double value) => Opacity = Math.Clamp(value, 0.20, 1.0);

        public void AdjustOpacity(double delta) => Dispatcher.Invoke(() => SetOpacity(Opacity + delta));

        public void AdjustFontSize(double delta) =>
            Dispatcher.Invoke(() => NotesText.FontSize = Math.Clamp(NotesText.FontSize + delta, 9, 36));

        // ---------- Always on top / click-through ----------

        public void SetAlwaysOnTop(bool value)
        {
            Topmost = value;
            App.Settings.Current.AlwaysOnTop = value;
        }

        public void SetClickThrough(bool enabled)
        {
            IsClickThrough = enabled;
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            int exStyle = ClickThroughInterop.GetWindowLong(hwnd, ClickThroughInterop.GWL_EXSTYLE);
            if (enabled)
                exStyle |= ClickThroughInterop.WS_EX_TRANSPARENT | ClickThroughInterop.WS_EX_LAYERED;
            else
                exStyle &= ~ClickThroughInterop.WS_EX_TRANSPARENT;

            ClickThroughInterop.SetWindowLong(hwnd, ClickThroughInterop.GWL_EXSTYLE, exStyle);
            LockButton.Content = enabled ? "🔒" : "🔓";
        }

        // ---------- Attach / detach ----------

        public void AttachTo(IntPtr handle, string label, string position = "TopRight")
        {
            _attachedHandle = handle;
            AttachedWindowLabel = label;
            _attachedPosition = position;
            _tracker.Attach(handle);

            if (WindowEnumerator.TryGetRect(handle, out var rect))
                RepositionRelativeToTarget(rect);

            ShowOverlay();
            AttachmentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Detach()
        {
            _attachedHandle = IntPtr.Zero;
            AttachedWindowLabel = null;
            _tracker.Detach();
            AttachmentChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool IsAttached => _attachedHandle != IntPtr.Zero;

        public void SetAttachPosition(string position)
        {
            _attachedPosition = position;
            if (IsAttached && WindowEnumerator.TryGetRect(_attachedHandle, out var rect))
                RepositionRelativeToTarget(rect);
        }

        private void RepositionRelativeToTarget(RECT rect)
        {
            const int margin = 16;
            switch (_attachedPosition)
            {
                case "TopLeft":
                    Left = rect.Left + margin;
                    Top = rect.Top + margin;
                    break;
                case "TopRight":
                    Left = rect.Right - Width - margin;
                    Top = rect.Top + margin;
                    break;
                case "BottomLeft":
                    Left = rect.Left + margin;
                    Top = rect.Bottom - Height - margin;
                    break;
                case "BottomRight":
                    Left = rect.Right - Width - margin;
                    Top = rect.Bottom - Height - margin;
                    break;
                case "Center":
                    Left = rect.Left + (rect.Width - Width) / 2;
                    Top = rect.Top + (rect.Height - Height) / 2;
                    break;
                default: // Custom - leave wherever the user last dragged it
                    break;
            }

            KeepOnScreen();
        }

        private void PositionAtDefault()
        {
            var wa = SystemParameters.WorkArea;
            Left = wa.Right - Width - 24;
            Top = wa.Top + 24;
        }

        public void ResetPosition()
        {
            _attachedPosition = "TopRight";
            PositionAtDefault();
        }

        private void KeepOnScreen()
        {
            var wa = SystemParameters.WorkArea;
            if (Left < wa.Left) Left = wa.Left;
            if (Top < wa.Top) Top = wa.Top;
            if (Left + Width > wa.Right) Left = Math.Max(wa.Left, wa.Right - Width);
            if (Top + Height > wa.Bottom) Top = Math.Max(wa.Top, wa.Bottom - Height);
        }

        // ---------- Theme ----------

        public void ApplyTheme(string theme, string customBg, string customText)
        {
            Color bg, text;
            switch (theme)
            {
                case "Light":
                    bg = (Color)ColorConverter.ConvertFromString("#F5F5F7");
                    text = (Color)ColorConverter.ConvertFromString("#1A1A1A");
                    break;
                case "Custom":
                    bg = (Color)ColorConverter.ConvertFromString(customBg);
                    text = (Color)ColorConverter.ConvertFromString(customText);
                    break;
                default: // Dark
                    bg = (Color)ColorConverter.ConvertFromString("#1E1F24");
                    text = (Color)ColorConverter.ConvertFromString("#F0F0F0");
                    break;
            }
            RootPanel.Background = new SolidColorBrush(bg);
            NotesText.Foreground = new SolidColorBrush(text);
        }

        // ---------- Drag / window chrome ----------

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                // Dragging manually overrides "attach position" - becomes Custom.
                _attachedPosition = "Custom";
                DragMove();
                SaveViewStateToProfile();
            }
        }

        private void Hide_Click(object sender, RoutedEventArgs e) => HideOverlay();

        private void Close_Click(object sender, RoutedEventArgs e) => Detach();

        private void LockButton_Click(object sender, RoutedEventArgs e) => SetClickThrough(!IsClickThrough);

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            App.Settings.Current.LastOverlayX = Left;
            App.Settings.Current.LastOverlayY = Top;
            SaveViewStateToProfile();
            base.OnClosing(e);
        }
    }

    internal static class ClickThroughInterop
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
