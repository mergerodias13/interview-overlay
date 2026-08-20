using System;
using System.Collections.Generic;

namespace InterviewOverlay.Models
{
    /// <summary>
    /// A saved interview profile: notes plus the user's preferred overlay
    /// look/position for that interview. Everything is stored locally.
    /// </summary>
    public class InterviewProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string InterviewName { get; set; } = "New Interview";
        public string Company { get; set; } = "";
        public string Position { get; set; } = "";
        public string NotesPlainText { get; set; } = "";
        public string NotesRichText { get; set; } = ""; // XAML FlowDocument, optional

        public double OverlayWidth { get; set; } = 380;
        public double OverlayHeight { get; set; } = 420;
        public double OverlayOpacity { get; set; } = 0.65;
        public double FontSize { get; set; } = 15;
        public string OverlayPosition { get; set; } = "TopRight"; // TopLeft/TopRight/BottomLeft/BottomRight/Center/Custom
        public double? CustomX { get; set; }
        public double? CustomY { get; set; }

        public string PreferredAppHint { get; set; } = ""; // e.g. "Zoom", "Teams", "Google Meet"

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

        public override string ToString() =>
            string.IsNullOrWhiteSpace(Company) ? InterviewName : $"{Company} — {Position}";
    }

    public class AppState
    {
        public List<InterviewProfile> Profiles { get; set; } = new();
        public string? LastOpenedProfileId { get; set; }
    }
}
