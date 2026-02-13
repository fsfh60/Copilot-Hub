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

    private void CopilotInput_KeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+Enter sends, plain Enter adds newline
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control
            && sender is TextBox tb && !string.IsNullOrWhiteSpace(tb.Text))
        {
            ViewModel?.SendCopilotInputCommand.Execute(tb.Text);
            tb.Clear();
            e.Handled = true;
        }
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        // Clear input after send — find within the visual tree
        if (sender is Button btn && btn.Parent is Grid grid)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(grid))
            {
                if (child is TextBox tb && tb.Name == "CopilotInput")
                {
                    tb.Clear();
                    break;
                }
            }
        }
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