using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopilotHub.App.Services;
using CopilotHub.Core.Models;
using CopilotHub.Core.Services;
using Serilog;

namespace CopilotHub.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISessionManager _sessionManager;
    private readonly ICopilotProcessService _copilotService;
    private readonly IFileChangeTracker _fileChangeTracker;
    private readonly IGitService _gitService;
    private readonly IDiffService _diffService;
    private readonly ITerminalService _terminalService;
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

    public MainViewModel(
        ISessionManager sessionManager,
        ICopilotProcessService copilotService,
        IFileChangeTracker fileChangeTracker,
        IGitService gitService,
        IDiffService diffService,
        ITerminalService terminalService,
        INotificationService notificationService,
        IDispatcherService dispatcher,
        ThemeService theme)
    {
        _sessionManager = sessionManager;
        _copilotService = copilotService;
        _fileChangeTracker = fileChangeTracker;
        _gitService = gitService;
        _diffService = diffService;
        _terminalService = terminalService;
        _notificationService = notificationService;
        _dispatcher = dispatcher;
        Theme = theme;

        _sessionManager.SessionCompleted += OnSessionCompleted;
        _fileChangeTracker.FileChanged += OnFileChanged;
        _terminalService.OutputReceived += OnTerminalOutput;
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

            try { await _terminalService.StartAsync(session.Id, dialog.SelectedDirectory); }
            catch (Exception ex) { _logger.Warning(ex, "Failed to start terminal for session {Id}", session.Id); }

            _ = Task.Run(async () =>
            {
                try { await _copilotService.StartSessionAsync(session); }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Copilot process failed for session {Id}", session.Id);
                    _dispatcher.Invoke(() => session.OutputLog.Add($"[ERROR] Copilot failed: {ex.Message}"));
                }
            });
        }
        catch (Exception ex) { _logger.Error(ex, "Failed to create new session"); }
    }

    [RelayCommand]
    private void CloseSession(SessionTabViewModel? tab)
    {
        if (tab is null) return;
        _copilotService.StopSession(tab.Session.Id);
        _fileChangeTracker.StopTracking(tab.Session.Id);
        _terminalService.Stop(tab.Session.Id);
        _sessionManager.RemoveSession(tab.Session.Id);
        Sessions.Remove(tab);
        if (SelectedSession == tab)
            SelectedSession = Sessions.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SendCopilotInputAsync(string? input)
    {
        if (string.IsNullOrWhiteSpace(input) || SelectedSession is null) return;
        _ = Task.Run(async () =>
        {
            try { await _copilotService.SendInputAsync(SelectedSession.Session.Id, input); }
            catch (Exception ex) { _logger.Error(ex, "Error sending copilot input"); }
        });
    }

    [RelayCommand]
    private async Task SendTerminalCommandAsync(string? command)
    {
        if (string.IsNullOrWhiteSpace(command) || SelectedSession is null) return;
        try { await _terminalService.SendCommandAsync(SelectedSession.Session.Id, command); }
        catch (Exception ex) { _logger.Error(ex, "Error sending terminal command"); }
    }

    [RelayCommand]
    private void OpenFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || SelectedSession is null) return;
        SelectedSession.OpenFile(filePath);
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

    private void OnTerminalOutput(object? sender, TerminalOutputEventArgs e)
    {
        _dispatcher.Invoke(() =>
        {
            var tab = Sessions.FirstOrDefault(s => s.Session.Id == e.SessionId);
            tab?.TerminalOutput.Add(e.Text);
        });
    }
}
