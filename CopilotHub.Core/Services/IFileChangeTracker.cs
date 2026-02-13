namespace CopilotHub.Core.Services;

public interface IFileChangeTracker : IDisposable
{
    void StartTracking(Guid sessionId, string directoryPath);
    void StopTracking(Guid sessionId);
    event EventHandler<FileChangedEventArgs>? FileChanged;
}

public class FileChangedEventArgs : EventArgs
{
    public Guid SessionId { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public FileChangeType ChangeType { get; init; }
}

public enum FileChangeType
{
    Created,
    Modified,
    Deleted,
    Renamed
}
