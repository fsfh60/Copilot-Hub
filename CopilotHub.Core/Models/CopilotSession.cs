using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CopilotHub.Core.Models;

public partial class CopilotSession : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();

    [ObservableProperty]
    private string _workingDirectory = string.Empty;

    [ObservableProperty]
    private SessionStatus _status = SessionStatus.Running;

    [ObservableProperty]
    private string _name = "New Session";

    [ObservableProperty]
    private bool _hasFileChanges;

    [ObservableProperty]
    private string _copilotModel = "claude-opus-4.6";

    [ObservableProperty]
    private string _copilotExtraArgs = "--allow-all";

    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public ObservableCollection<string> OutputLog { get; } = [];

    public ObservableCollection<string> ModifiedFiles { get; } = [];
}
