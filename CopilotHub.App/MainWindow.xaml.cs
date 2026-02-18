using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CopilotHub.App.Controls;
using CopilotHub.App.ViewModels;

namespace CopilotHub.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.ConsoleSwapRequested += OnConsoleSwapRequested;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
        UpdateVisibility();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Save any pending editor content before closing
        SyncEditorToModel();

        if (ViewModel is not null)
        {
            ViewModel.ConsoleSwapRequested -= OnConsoleSwapRequested;
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            foreach (var session in ViewModel.Sessions)
                session.KillAllProcesses();
        }
        ConsoleHost.ReleaseConsole();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedSession))
            Dispatcher.Invoke(LoadEditorForSelectedFile);
    }

    private void OnConsoleSwapRequested()
    {
        Dispatcher.Invoke(SwapToActiveConsole);
    }

    private void SwapToActiveConsole()
    {
        var session = ViewModel?.SelectedSession;
        UpdateVisibility();

        if (session is null || session.IsFileEditorActive)
        {
            ConsoleHost.ReleaseConsole();
            return;
        }

        var hwnd = session.GetActiveConsoleHwnd();
        if (hwnd != IntPtr.Zero)
        {
            ConsoleHost.EmbedConsoleWindow(hwnd);
            ConsoleHost.FocusConsole();
        }
        else
        {
            ConsoleHost.ReleaseConsole();
        }
    }

    private void UpdateVisibility()
    {
        var hasSession = ViewModel?.SelectedSession is not null;
        var isEditing = ViewModel?.SelectedSession?.IsFileEditorActive == true;

        NoSessionPlaceholder.Visibility = hasSession ? Visibility.Collapsed : Visibility.Visible;
        FileEditorPanel.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
        ConsoleHost.Visibility = (hasSession && !isEditing) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SessionTab_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is SessionTabViewModel tab && ViewModel is not null)
        {
            SyncEditorToModel();
            ViewModel.SelectedSession = tab;
        }
    }

    private void ConsoleType_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string typeStr && ViewModel is not null)
        {
            ViewModel.SwitchConsoleTypeCommand.Execute(typeStr);
        }
    }

    private void BackToConsole_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedSession is not null)
        {
            SyncEditorToModel();
            ViewModel.SelectedSession.IsFileEditorActive = false;
            SwapToActiveConsole();
        }
    }

    private void FileTab_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is OpenFileTab file && ViewModel?.SelectedSession is not null)
        {
            SyncEditorToModel();
            ViewModel.SelectedSession.SelectedFile = file;
            LoadEditorForSelectedFile();
        }
    }

    private void ShowEditView_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedSession?.SelectedFile is not null)
            ViewModel.SelectedSession.SelectedFile.IsDiffView = false;
        AvalonEditor.Visibility = Visibility.Visible;
        DiffViewPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowDiffView_Click(object sender, RoutedEventArgs e)
    {
        SyncEditorToModel();
        var file = ViewModel?.SelectedSession?.SelectedFile;
        if (file is null) return;

        file.IsDiffView = true;
        AvalonEditor.Visibility = Visibility.Collapsed;
        DiffViewPanel.Visibility = Visibility.Visible;

        DiffOriginalEditor.Text = file.OriginalContent;
        DiffModifiedEditor.Text = file.Content;
    }

    /// <summary>Load the currently selected file into the AvalonEdit editor.</summary>
    private void LoadEditorForSelectedFile()
    {
        var file = ViewModel?.SelectedSession?.SelectedFile;
        if (file is null) return;

        if (file.IsDiffView)
        {
            AvalonEditor.Visibility = Visibility.Collapsed;
            DiffViewPanel.Visibility = Visibility.Visible;
            DiffOriginalEditor.Text = file.OriginalContent;
            DiffModifiedEditor.Text = file.Content;
        }
        else
        {
            AvalonEditor.Visibility = Visibility.Visible;
            DiffViewPanel.Visibility = Visibility.Collapsed;
            AvalonEditor.Text = file.Content;
        }
    }

    /// <summary>Sync AvalonEdit text back to the view model.</summary>
    private void SyncEditorToModel()
    {
        var file = ViewModel?.SelectedSession?.SelectedFile;
        if (file is not null && !file.IsDiffView)
        {
            var editorText = AvalonEditor.Text;
            if (editorText != file.Content)
                file.Content = editorText;
        }
    }
}