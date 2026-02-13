using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopilotHub.Core.Models;

namespace CopilotHub.App.ViewModels;

public partial class SessionTabViewModel : ObservableObject
{
    public CopilotSession Session { get; }
    public string Name => Session.Name;
    public ObservableCollection<string> OutputLog => Session.OutputLog;
    public ObservableCollection<string> ModifiedFiles => Session.ModifiedFiles;
    public ObservableCollection<string> TerminalOutput { get; } = [];
    public ObservableCollection<OpenFileTab> OpenFiles { get; } = [];

    [ObservableProperty]
    private bool _isFlashing;

    [ObservableProperty]
    private bool _hasFileIndicator;

    [ObservableProperty]
    private string _copilotInputText = string.Empty;

    [ObservableProperty]
    private string _terminalInput = string.Empty;

    [ObservableProperty]
    private OpenFileTab? _selectedFile;

    // false = copilot output, true = file editor
    [ObservableProperty]
    private bool _isFileEditorActive;

    public SessionTabViewModel(CopilotSession session)
    {
        Session = session;
    }

    public void OpenFile(string relativePath)
    {
        // Check if already open
        var existing = OpenFiles.FirstOrDefault(f => f.RelativePath == relativePath);
        if (existing is not null)
        {
            SelectedFile = existing;
            IsFileEditorActive = true;
            return;
        }

        var fullPath = Path.Combine(Session.WorkingDirectory, relativePath);
        if (!File.Exists(fullPath)) return;

        var content = File.ReadAllText(fullPath);
        var tab = new OpenFileTab(relativePath, fullPath, content);
        OpenFiles.Add(tab);
        SelectedFile = tab;
        IsFileEditorActive = true;
    }

    [RelayCommand]
    private void CloseFile(OpenFileTab? file)
    {
        if (file is null) return;

        // Save if modified
        if (file.IsModified)
            File.WriteAllText(file.FullPath, file.Content);

        OpenFiles.Remove(file);
        SelectedFile = OpenFiles.FirstOrDefault();
        if (SelectedFile is null)
            IsFileEditorActive = false;
    }

    [RelayCommand]
    private void ShowCopilotOutput()
    {
        IsFileEditorActive = false;
    }

    [RelayCommand]
    private void SaveFile(OpenFileTab? file)
    {
        if (file is null) return;
        File.WriteAllText(file.FullPath, file.Content);
        file.IsModified = false;
    }
}

public partial class OpenFileTab : ObservableObject
{
    public string RelativePath { get; }
    public string FullPath { get; }
    public string FileName => Path.GetFileName(RelativePath);

    [ObservableProperty]
    private string _content;

    [ObservableProperty]
    private bool _isModified;

    public OpenFileTab(string relativePath, string fullPath, string content)
    {
        RelativePath = relativePath;
        FullPath = fullPath;
        _content = content;
    }

    partial void OnContentChanged(string value)
    {
        IsModified = true;
    }
}
