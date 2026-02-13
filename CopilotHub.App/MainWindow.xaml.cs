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
        if (e.Key == Key.Enter && sender is TextBox tb && !string.IsNullOrWhiteSpace(tb.Text))
        {
            ViewModel?.SendCopilotInputCommand.Execute(tb.Text);
            tb.Clear();
            e.Handled = true;
        }
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var input = FindName("CopilotInput") as TextBox;
        input?.Clear();
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
}