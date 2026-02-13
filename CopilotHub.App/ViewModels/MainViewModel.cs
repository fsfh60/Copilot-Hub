using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopilotHub.App.Controls;
using CopilotHub.App.Services;
using CopilotHub.Core.Models;
using CopilotHub.Core.Services;
using Serilog;

namespace CopilotHub.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISessionManager _sessionManager;
    private readonly IFileChangeTracker _fileChangeTracker;
    private readonly IGitService _gitService;
    private readonly IDiffService _diffService;
    private readonly INotificationService _notificationService;
    private readonly IDispatcherService _dispatcher;
    private readonly ILogger _logger = Log.ForContext<MainViewModel>();

    public ThemeService Theme { get; }

    public ObservableCollection<SessionTabViewModel> Sessions { get; } = [];

    [ObservableProperty]
    private SessionTabViewModel? _selectedSession;

    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    private string _originalContent = string.Empty;

    [ObservableProperty]
    private string _modifiedContent = string.Empty;

    [ObservableProperty]
    private string _diffText = string.Empty;

    [ObservableProperty]
    private bool _isDiffVisible;

    /// <summary>Raised when the active console should be swapped in the UI.</summary>
    public event Action? ConsoleSwapRequested;

    public MainViewModel(
        ISessionManager sessionManager,
        IFileChangeTracker fileChangeTracker,
        IGitService gitService,
        IDiffService diffService,
        INotificationService notificationService,
        IDispatcherService dispatcher,
        ThemeService theme)
    {
        _sessionManager = sessionManager;
        _fileChangeTracker = fileChangeTracker;
        _gitService = gitService;
        _diffService = diffService;
        _notificationService = notificationService;
        _dispatcher = dispatcher;
        Theme = theme;

        _sessionManager.SessionCompleted += OnSessionCompleted;
        _fileChangeTracker.FileChanged += OnFileChanged;
    }

    partial void OnSelectedSessionChanged(SessionTabViewModel? value)
    {
        ConsoleSwapRequested?.Invoke();
    }

    [RelayCommand]
    private async Task NewSessionAsync()
    {
        try
        {
            var dialog = new Views.NewSessionDialog
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (dialog.ShowDialog() != true) return;

            var session = _sessionManager.CreateSession(dialog.SelectedDirectory, dialog.SessionName);
            session.CopilotModel = dialog.SelectedModel;
            session.CopilotExtraArgs = dialog.ExtraArgs;

            var tabVm = new SessionTabViewModel(session);
            Sessions.Add(tabVm);
            SelectedSession = tabVm;

            _fileChangeTracker.StartTracking(session.Id, dialog.SelectedDirectory);

            // Start native console processes in background
            _ = StartNativeConsolesAsync(tabVm);
        }
        catch (Exception ex) { _logger.Error(ex, "Failed to create new session"); }
    }

    private async Task StartNativeConsolesAsync(SessionTabViewModel tabVm)
    {
        var dir = tabVm.Session.WorkingDirectory;
        var model = tabVm.Session.CopilotModel;
        var extraArgs = tabVm.Session.CopilotExtraArgs;

        // Find PowerShell 7+ or fall back to Windows PowerShell
        var pwshExe = File.Exists(@"C:\Program Files\PowerShell\7\pwsh.exe")
            ? @"C:\Program Files\PowerShell\7\pwsh.exe" : "powershell.exe";

        // Start CMD
        var (cmdProc, cmdHwnd) = await NativeConsoleHost.StartConsoleProcessAsync("cmd.exe", "", dir);
        if (cmdProc != null)
            tabVm.ConsoleProcesses.Add(new ConsoleProcessInfo
                { Type = ConsoleType.Cmd, Process = cmdProc, ConsoleHwnd = cmdHwnd });

        // Start PowerShell
        var (psProc, psHwnd) = await NativeConsoleHost.StartConsoleProcessAsync(pwshExe, $"-NoLogo -WorkingDirectory \"{dir}\"", dir);
        if (psProc != null)
            tabVm.ConsoleProcesses.Add(new ConsoleProcessInfo
                { Type = ConsoleType.PowerShell, Process = psProc, ConsoleHwnd = psHwnd });

        // Start Copilot CLI (real interactive session)
        var copilotArgs = $"--model {model}";
        if (!string.IsNullOrWhiteSpace(extraArgs))
            copilotArgs += $" {extraArgs}";
        var (copilotProc, copilotHwnd) = await NativeConsoleHost.StartConsoleProcessAsync("copilot", copilotArgs, dir);
        if (copilotProc != null)
            tabVm.ConsoleProcesses.Add(new ConsoleProcessInfo
                { Type = ConsoleType.Copilot, Process = copilotProc, ConsoleHwnd = copilotHwnd });

        // Notify UI to embed the active console
        _dispatcher.Invoke(() => ConsoleSwapRequested?.Invoke());
    }

    [RelayCommand]
    private void CloseSession(SessionTabViewModel? tab)
    {
        if (tab is null) return;
        tab.KillAllProcesses();
        _fileChangeTracker.StopTracking(tab.Session.Id);
        _sessionManager.RemoveSession(tab.Session.Id);
        Sessions.Remove(tab);
        if (SelectedSession == tab)
            SelectedSession = Sessions.FirstOrDefault();
    }

    [RelayCommand]
    private void SwitchConsoleType(string? typeStr)
    {
        if (SelectedSession is null || typeStr is null) return;
        if (Enum.TryParse<ConsoleType>(typeStr, out var type))
        {
            SelectedSession.ActiveConsoleType = type;
            SelectedSession.IsFileEditorActive = false;
            ConsoleSwapRequested?.Invoke();
        }
    }

    [RelayCommand]
    private void OpenFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || SelectedSession is null) return;
        SelectedSession.OpenFile(filePath);
        ConsoleSwapRequested?.Invoke();
    }

    [RelayCommand]
    private void ViewFileDiff(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || SelectedSession is null) return;
        try
        {
            var session = SelectedSession.Session;
            var repoPath = session.WorkingDirectory;

            if (_gitService.IsGitRepository(repoPath))
            {
                var diffResult = _gitService.GetFileDiff(repoPath, filePath);
                if (diffResult is not null)
                {
                    var computed = _diffService.ComputeDiff(diffResult.OriginalContent, diffResult.ModifiedContent, filePath);
                    OriginalContent = computed.OriginalContent;
                    ModifiedContent = computed.ModifiedContent;
                    DiffText = computed.UnifiedDiff;
                    SelectedFilePath = filePath;
                    IsDiffVisible = true;
                    return;
                }
            }

            var fullPath = Path.Combine(repoPath, filePath);
            if (File.Exists(fullPath))
            {
                var content = File.ReadAllText(fullPath);
                OriginalContent = string.Empty;
                ModifiedContent = content;
                DiffText = content;
                SelectedFilePath = filePath;
                IsDiffVisible = true;
            }
        }
        catch (Exception ex) { _logger.Error(ex, "Error viewing diff for {File}", filePath); }
    }

    [RelayCommand]
    private void CloseDiff()
    {
        IsDiffVisible = false;
        SelectedFilePath = null;
        OriginalContent = string.Empty;
        ModifiedContent = string.Empty;
        DiffText = string.Empty;
    }

    private void OnSessionCompleted(object? sender, Core.Events.SessionCompletedEventArgs e)
    {
        _dispatcher.Invoke(() =>
        {
            var tab = Sessions.FirstOrDefault(s => s.Session.Id == e.Session.Id);
            if (tab is not null) tab.IsFlashing = true;
            _notificationService.ShowSessionCompleted(e.Session.Name, e.FinalStatus == SessionStatus.Completed);
        });
    }

    private void OnFileChanged(object? sender, FileChangedEventArgs e)
    {
        _dispatcher.Invoke(() =>
        {
            var session = _sessionManager.GetSession(e.SessionId);
            if (session is null) return;
            var relativePath = Path.GetRelativePath(session.WorkingDirectory, e.FilePath);
            if (!session.ModifiedFiles.Contains(relativePath))
            {
                session.ModifiedFiles.Add(relativePath);
                session.HasFileChanges = true;
            }
            var tab = Sessions.FirstOrDefault(s => s.Session.Id == e.SessionId);
            if (tab is not null) tab.HasFileIndicator = true;
        });
    }
}
