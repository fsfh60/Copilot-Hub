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
            ViewModel.ConsoleSwapRequested += OnConsoleSwapRequested;
        UpdateVisibility();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Kill all session processes on close
        if (ViewModel is not null)
        {
            ViewModel.ConsoleSwapRequested -= OnConsoleSwapRequested;
            foreach (var session in ViewModel.Sessions)
                session.KillAllProcesses();
        }
        ConsoleHost.ReleaseConsole();
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
            ViewModel.SelectedSession.IsFileEditorActive = false;
            SwapToActiveConsole();
        }
    }
}