using System.Windows;
using System.Windows.Controls;
using InterviewOverlay.WindowManagement;

namespace InterviewOverlay.Views
{
    public partial class AttachWindow : Window
    {
        public WindowInfo? SelectedWindow { get; private set; }
        public string SelectedPosition { get; private set; } = "TopRight";

        public AttachWindow()
        {
            InitializeComponent();
            RefreshList();
        }

        private void RefreshList()
        {
            var windows = WindowEnumerator.GetOpenWindows();
            WindowList.ItemsSource = windows;
            WindowList.DisplayMemberPath = nameof(WindowInfo.DisplayLabel);
            if (windows.Count > 0) WindowList.SelectedIndex = 0;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshList();

        private void Attach_Click(object sender, RoutedEventArgs e)
        {
            if (WindowList.SelectedItem is not WindowInfo info)
            {
                MessageBox.Show(this, "Select a window first.", "Interview Overlay",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedWindow = info;
            SelectedPosition = (PositionCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "TopRight";
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
