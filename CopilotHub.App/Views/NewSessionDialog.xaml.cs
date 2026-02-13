using System.IO;
using System.Windows;

namespace CopilotHub.App.Views;

public partial class NewSessionDialog : Window
{
    public string SelectedDirectory { get; private set; } = string.Empty;
    public string SelectedModel { get; private set; } = "claude-opus-4.6";
    public string ExtraArgs { get; private set; } = "--allow-all";
    public string SessionName { get; private set; } = string.Empty;

    public NewSessionDialog()
    {
        InitializeComponent();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Working Directory"
        };

        if (dialog.ShowDialog(this) == true)
        {
            DirectoryBox.Text = dialog.FolderName;
        }
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DirectoryBox.Text) || !Directory.Exists(DirectoryBox.Text))
        {
            MessageBox.Show("Please select a valid working directory.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedDirectory = DirectoryBox.Text;
        SelectedModel = ModelBox.Text;
        ExtraArgs = ExtraArgsBox.Text;
        SessionName = string.IsNullOrWhiteSpace(SessionNameBox.Text)
            ? Path.GetFileName(DirectoryBox.Text) ?? "Session"
            : SessionNameBox.Text;

        DialogResult = true;
    }
}
