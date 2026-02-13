using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CopilotHub.App.ViewModels;

namespace CopilotHub.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void CopilotInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift
            && sender is TextBox tb && !string.IsNullOrWhiteSpace(tb.Text))
        {
            ViewModel?.SendCopilotInputCommand.Execute(tb.Text);
            // Clear via the bound property
            if (tb.DataContext is SessionTabViewModel stvm)
                stvm.CopilotInputText = string.Empty;
            e.Handled = true;
        }
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        // Clear the input after send
        if (ViewModel?.SelectedSession is SessionTabViewModel stvm)
            stvm.CopilotInputText = string.Empty;
    }

    private void TerminalInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox tb && !string.IsNullOrWhiteSpace(tb.Text))
        {
            ViewModel?.SendTerminalCommandCommand.Execute(tb.Text);
            tb.Clear();
            e.Handled = true;
        }
    }

    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
        TerminalInput?.Clear();
    }

    private void FileTab_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is OpenFileTab fileTab
            && ViewModel?.SelectedSession is not null)
        {
            ViewModel.SelectedSession.SelectedFile = fileTab;
        }
    }
}