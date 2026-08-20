using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using InterviewOverlay.Models;
using Microsoft.Win32;

namespace InterviewOverlay
{
    public partial class MainWindow : Window
    {
        private InterviewProfile? _current;
        private DispatcherTimer? _autoSaveTimer;
        private bool _suppressChangeEvents;

        public MainWindow()
        {
            InitializeComponent();
            LoadProfilesIntoList();

            var last = App.Notes.GetLastOpened();
            if (last != null)
            {
                ProfileList.SelectedItem = App.Notes.State.Profiles.FirstOrDefault(p => p.Id == last.Id);
            }
            else if (ProfileList.Items.Count > 0)
            {
                ProfileList.SelectedIndex = 0;
            }

            StartAutoSave();
            RefreshAttachedLabel();
        }

        private void LoadProfilesIntoList()
        {
            ProfileList.ItemsSource = null;
            ProfileList.ItemsSource = App.Notes.State.Profiles;
            ProfileList.DisplayMemberPath = "";
        }

        private void StartAutoSave()
        {
            var seconds = Math.Max(2, App.Settings.Current.AutoSaveIntervalSeconds);
            _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
            _autoSaveTimer.Tick += (_, _) => SaveCurrentProfile();
            _autoSaveTimer.Start();
        }

        // ---------- Profile selection / editing ----------

        private void ProfileList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SaveCurrentProfile();

            if (ProfileList.SelectedItem is InterviewProfile profile)
            {
                _current = profile;
                _suppressChangeEvents = true;
                InterviewNameBox.Text = profile.InterviewName;
                CompanyBox.Text = profile.Company;
                PositionBox.Text = profile.Position;
                NotesEditor.Text = profile.NotesPlainText;
                _suppressChangeEvents = false;

                App.Notes.Touch(profile);
                App.Overlay?.LoadProfile(profile);
            }
        }

        private void MetaField_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_suppressChangeEvents || _current == null) return;
            _current.InterviewName = string.IsNullOrWhiteSpace(InterviewNameBox.Text) ? "Untitled Interview" : InterviewNameBox.Text;
            _current.Company = CompanyBox.Text;
            _current.Position = PositionBox.Text;
            RefreshListDisplay();
        }

        private void NotesEditor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_suppressChangeEvents || _current == null) return;
            _current.NotesPlainText = NotesEditor.Text;
            App.Overlay?.RefreshNotesText(NotesEditor.Text);
        }

        private void RefreshListDisplay()
        {
            var idx = ProfileList.SelectedIndex;
            ProfileList.Items.Refresh();
            ProfileList.SelectedIndex = idx;
        }

        private void SaveCurrentProfile()
        {
            if (_current == null) return;
            App.Overlay?.SaveViewStateToProfile();
            App.Notes.Save();
        }

        // ---------- Toolbar actions ----------

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_current == null)
            {
                MessageBox.Show(this, "Select a profile first.", "Interview Overlay",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (App.Notes.State.Profiles.Count <= 1)
            {
                MessageBox.Show(this, "You need at least one profile - create a new one before deleting this.",
                    "Interview Overlay", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(this,
                $"Delete \"{_current}\"? This can't be undone.",
                "Delete Profile", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var idToDelete = _current.Id;
            App.Notes.DeleteProfile(idToDelete);
            _current = null;

            LoadProfilesIntoList();
            if (ProfileList.Items.Count > 0)
                ProfileList.SelectedIndex = 0;
        }

        private void NewNotes_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentProfile();
            var profile = App.Notes.CreateProfile("New Interview");
            LoadProfilesIntoList();
            ProfileList.SelectedItem = profile;
        }

        private void OpenNotes_Click(object sender, RoutedEventArgs e)
        {
            NotesEditor.Focus();
        }

        public void OpenAttachDialog()
        {
            var dlg = new Views.AttachWindow { Owner = this };
            if (dlg.ShowDialog() == true && dlg.SelectedWindow != null)
            {
                var position = dlg.SelectedPosition;
                App.Overlay?.AttachTo(dlg.SelectedWindow.Handle, dlg.SelectedWindow.DisplayLabel, position);
                if (_current != null) _current.OverlayPosition = position;
                RefreshAttachedLabel();
            }
        }

        private void Attach_Click(object sender, RoutedEventArgs e) => OpenAttachDialog();

        private void Detach_Click(object sender, RoutedEventArgs e)
        {
            App.Overlay?.Detach();
            RefreshAttachedLabel();
        }

        private void ToggleOverlay_Click(object sender, RoutedEventArgs e) => App.Overlay?.ToggleVisibility();

        public void OpenSettings()
        {
            var dlg = new Views.SettingsWindow { Owner = this };
            dlg.ShowDialog();
        }

        private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_current == null) return;
            SaveCurrentProfile();

            var dlg = new SaveFileDialog
            {
                Filter = "JSON (*.json)|*.json|Markdown (*.md)|*.md|Text (*.txt)|*.txt",
                FileName = _current.InterviewName
            };
            if (dlg.ShowDialog() == true)
            {
                App.Notes.ExportProfile(_current, dlg.FileName);
                MessageBox.Show(this, "Notes exported.", "Interview Overlay", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Supported files (*.json;*.md;*.txt)|*.json;*.md;*.txt|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                var profile = App.Notes.ImportProfile(dlg.FileName);
                LoadProfilesIntoList();
                ProfileList.SelectedItem = profile;
            }
        }

        private void RefreshAttachedLabel()
        {
            AttachedLabel.Text = App.Overlay?.IsAttached == true
                ? App.Overlay.AttachedWindowLabel
                : "None";
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (App.Overlay != null)
                App.Overlay.AttachmentChanged += (_, _) => RefreshAttachedLabel();
        }

        // ---------- Close = minimize to tray (unless exiting) ----------

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveCurrentProfile();

            if (App.Settings.Current.MinimizeToTray && !AppIsExiting)
            {
                e.Cancel = true;
                Hide();
            }
        }

        public static bool AppIsExiting { get; set; } = false;
    }
}