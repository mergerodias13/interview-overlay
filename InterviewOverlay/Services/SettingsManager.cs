using System;
using System.IO;
using System.Text.Json;
using InterviewOverlay.Models;

namespace InterviewOverlay.Services
{
    /// <summary>
    /// Loads and persists app settings as local JSON. No network calls,
    /// no accounts, nothing leaves the machine.
    /// </summary>
    public class SettingsManager
    {
        public AppSettings Current { get; private set; } = new();

        private static string AppFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "InterviewOverlay");

        private static string SettingsPath => Path.Combine(AppFolder, "settings.json");

        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

        public void Load()
        {
            try
            {
                Directory.CreateDirectory(AppFolder);
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                // Corrupt settings file shouldn't crash the app on launch.
                Current = new AppSettings();
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(AppFolder);
                var json = JsonSerializer.Serialize(Current, JsonOpts);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Best effort - don't crash on exit if disk write fails.
            }
        }
    }
}
