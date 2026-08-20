using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace InterviewOverlay.WindowManagement
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; } = "";
        public string ProcessName { get; set; } = "";
        public MeetingApp DetectedApp { get; set; } = MeetingApp.Other;

        public string DisplayLabel =>
            DetectedApp == MeetingApp.Other ? $"{ProcessName} — {Title}" : $"{DetectedApp} — {Title}";
    }

    public enum MeetingApp
    {
        Other,
        Zoom,
        MicrosoftTeams,
        GoogleMeetChrome,
        GoogleMeetEdge
    }

    /// <summary>
    /// Enumerates visible top-level windows so the user can pick one to
    /// attach the overlay to. Does best-effort detection of common meeting
    /// apps by process name / window title, but always allows manual
    /// selection - browser titles change often, so we never rely on them
    /// exclusively.
    /// </summary>
    public static class WindowEnumerator
    {
        public static List<WindowInfo> GetOpenWindows()
        {
            var results = new List<WindowInfo>();
            IntPtr shellWindow = NativeMethods.GetShellWindow();

            NativeMethods.EnumWindows((hWnd, _) =>
            {
                if (hWnd == shellWindow) return true;
                if (!NativeMethods.IsWindowVisible(hWnd)) return true;

                int length = NativeMethods.GetWindowTextLength(hWnd);
                if (length == 0) return true;

                var sb = new StringBuilder(length + 1);
                NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();
                if (string.IsNullOrWhiteSpace(title)) return true;

                // Skip tool windows (small utility windows, not real app windows).
                int exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
                bool isToolWindow = (exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0;
                bool isAppWindow = (exStyle & NativeMethods.WS_EX_APPWINDOW) != 0;
                if (isToolWindow && !isAppWindow) return true;

                NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
                string processName = "";
                try
                {
                    using var proc = Process.GetProcessById((int)pid);
                    processName = proc.ProcessName;
                }
                catch { /* process may have exited between enum and lookup */ }

                if (string.IsNullOrEmpty(processName)) return true;

                var info = new WindowInfo
                {
                    Handle = hWnd,
                    Title = title,
                    ProcessName = processName,
                    DetectedApp = DetectMeetingApp(processName, title)
                };
                results.Add(info);
                return true;
            }, IntPtr.Zero);

            // Meeting apps first, most useful to the user.
            results.Sort((a, b) =>
            {
                int Rank(MeetingApp app) => app == MeetingApp.Other ? 1 : 0;
                return Rank(a.DetectedApp).CompareTo(Rank(b.DetectedApp));
            });

            return results;
        }

        private static MeetingApp DetectMeetingApp(string processName, string title)
        {
            string p = processName.ToLowerInvariant();
            string t = title.ToLowerInvariant();

            if (p.Contains("zoom")) return MeetingApp.Zoom;
            if (p.Contains("teams")) return MeetingApp.MicrosoftTeams;

            if (p.Contains("chrome") && t.Contains("meet")) return MeetingApp.GoogleMeetChrome;
            if (p.Contains("msedge") && t.Contains("meet")) return MeetingApp.GoogleMeetEdge;

            return MeetingApp.Other;
        }

        public static bool TryGetRect(IntPtr hWnd, out RECT rect) =>
            NativeMethods.GetWindowRect(hWnd, out rect);

        public static bool IsMinimized(IntPtr hWnd) => NativeMethods.IsIconic(hWnd);
        public static bool StillExists(IntPtr hWnd) => NativeMethods.IsWindow(hWnd);
    }
}
