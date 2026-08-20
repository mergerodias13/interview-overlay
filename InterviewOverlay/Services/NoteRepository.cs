using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using InterviewOverlay.Models;

namespace InterviewOverlay.Services
{
    /// <summary>
    /// Local-only storage for interview profiles/notes. Everything lives in
    /// %AppData%\InterviewOverlay\notes.json. No accounts, no network sync.
    /// </summary>
    public class NoteRepository
    {
        public AppState State { get; private set; } = new();

        private static string AppFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "InterviewOverlay");

        private static string DataPath => Path.Combine(AppFolder, "notes.json");
        private static string BackupPath => Path.Combine(AppFolder, "notes.backup.json");

        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

        public event EventHandler? Changed;

        public void Load()
        {
            try
            {
                Directory.CreateDirectory(AppFolder);
                if (File.Exists(DataPath))
                {
                    var json = File.ReadAllText(DataPath);
                    State = JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
                }
            }
            catch
            {
                // Try the crash-recovery backup before giving up.
                try
                {
                    if (File.Exists(BackupPath))
                    {
                        var json = File.ReadAllText(BackupPath);
                        State = JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
                    }
                }
                catch
                {
                    State = new AppState();
                }
            }

            if (State.Profiles.Count == 0)
            {
                State.Profiles.Add(new InterviewProfile
                {
                    InterviewName = "Sample Interview",
                    Company = "Acme Corp",
                    Position = "Software Engineer",
                    NotesPlainText =
                        "INTERVIEW NOTES\n\n" +
                        "Tell me about yourself\n- Current role\n- Previous experience\n- Key achievement\n\n" +
                        "Why do you want this position?\n- Company research\n- Connect experience to role\n\n" +
                        "STAR Example #1\nSituation:\nTask:\nAction:\nResult:\n\n" +
                        "Questions to ask\n- What does success look like?\n- What is the team structure?\n- What are the next steps?"
                });
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(AppFolder);
                // Keep a rolling backup so a bad write / crash mid-save can't
                // lose the previous good copy.
                if (File.Exists(DataPath))
                    File.Copy(DataPath, BackupPath, overwrite: true);

                var json = JsonSerializer.Serialize(State, JsonOpts);
                File.WriteAllText(DataPath, json);
                Changed?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                // Best effort - autosave should never crash the app.
            }
        }

        public InterviewProfile? GetLastOpened()
        {
            if (State.LastOpenedProfileId == null) return State.Profiles.FirstOrDefault();
            return State.Profiles.FirstOrDefault(p => p.Id == State.LastOpenedProfileId)
                   ?? State.Profiles.FirstOrDefault();
        }

        public InterviewProfile CreateProfile(string name = "New Interview")
        {
            var p = new InterviewProfile { InterviewName = name };
            State.Profiles.Add(p);
            State.LastOpenedProfileId = p.Id;
            Save();
            return p;
        }

        public void DeleteProfile(string id)
        {
            State.Profiles.RemoveAll(p => p.Id == id);
            Save();
        }

        public void Touch(InterviewProfile profile)
        {
            profile.UpdatedUtc = DateTime.UtcNow;
            State.LastOpenedProfileId = profile.Id;
        }

        public void ExportProfile(InterviewProfile profile, string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".json")
            {
                File.WriteAllText(filePath, JsonSerializer.Serialize(profile, JsonOpts));
            }
            else if (ext == ".md")
            {
                var md = $"# {profile.InterviewName}\n\n" +
                         $"**Company:** {profile.Company}  \n**Position:** {profile.Position}\n\n---\n\n" +
                         profile.NotesPlainText;
                File.WriteAllText(filePath, md);
            }
            else
            {
                File.WriteAllText(filePath, profile.NotesPlainText);
            }
        }

        public InterviewProfile ImportProfile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            InterviewProfile profile;
            if (ext == ".json")
            {
                var json = File.ReadAllText(filePath);
                profile = JsonSerializer.Deserialize<InterviewProfile>(json) ?? new InterviewProfile();
                profile.Id = Guid.NewGuid().ToString("N"); // avoid id collisions
            }
            else
            {
                profile = new InterviewProfile
                {
                    InterviewName = Path.GetFileNameWithoutExtension(filePath),
                    NotesPlainText = File.ReadAllText(filePath)
                };
            }

            State.Profiles.Add(profile);
            Save();
            return profile;
        }
    }
}
