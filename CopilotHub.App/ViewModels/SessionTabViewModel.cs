using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CopilotHub.Core.Models;

namespace CopilotHub.App.ViewModels;

public partial class SessionTabViewModel : ObservableObject
{
    public CopilotSession Session { get; }
    public string Name => Session.Name;
    public ObservableCollection<string> OutputLog => Session.OutputLog;
    public ObservableCollection<string> ModifiedFiles => Session.ModifiedFiles;
    public ObservableCollection<string> TerminalOutput { get; } = [];

    [ObservableProperty]
    private bool _isFlashing;

    [ObservableProperty]
    private bool _hasFileIndicator;

    [ObservableProperty]
    private string _terminalInput = string.Empty;

    public SessionTabViewModel(CopilotSession session)
    {
        Session = session;
    }
}
