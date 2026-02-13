using System.Collections.Concurrent;
using System.Timers;
using CopilotHub.Core.Services;
using Serilog;

namespace CopilotHub.Infrastructure.Services;

public class FileChangeTracker : IFileChangeTracker
{
    private readonly ConcurrentDictionary<Guid, FileSystemWatcher> _watchers = new();
    private readonly ConcurrentDictionary<string, DateTime> _recentChanges = new();
    private readonly ILogger _logger = Log.ForContext<FileChangeTracker>();
    private readonly TimeSpan _throttleWindow = TimeSpan.FromMilliseconds(500);
    private readonly System.Timers.Timer _cleanupTimer;

    public event EventHandler<FileChangedEventArgs>? FileChanged;

    public FileChangeTracker()
    {
        _cleanupTimer = new System.Timers.Timer(5000);
        _cleanupTimer.Elapsed += CleanupOldEntries;
        _cleanupTimer.Start();
    }

    public void StartTracking(Guid sessionId, string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            _logger.Warning("Directory {Path} does not exist", directoryPath);
            return;
        }

        StopTracking(sessionId);

        var watcher = new FileSystemWatcher(directoryPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        watcher.Changed += (_, e) => OnFileEvent(sessionId, e.FullPath, FileChangeType.Modified);
        watcher.Created += (_, e) => OnFileEvent(sessionId, e.FullPath, FileChangeType.Created);
        watcher.Deleted += (_, e) => OnFileEvent(sessionId, e.FullPath, FileChangeType.Deleted);
        watcher.Renamed += (_, e) => OnFileEvent(sessionId, e.FullPath, FileChangeType.Renamed);
        watcher.Error += (_, e) => _logger.Error(e.GetException(), "FileSystemWatcher error for session {SessionId}", sessionId);

        _watchers[sessionId] = watcher;
        _logger.Information("Started tracking {Path} for session {SessionId}", directoryPath, sessionId);
    }

    public void StopTracking(Guid sessionId)
    {
        if (_watchers.TryRemove(sessionId, out var watcher))
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }

    private void OnFileEvent(Guid sessionId, string filePath, FileChangeType changeType)
    {
        // Skip .git internals and common build artifacts
        if (ShouldIgnore(filePath)) return;

        // Throttle rapid changes to the same file
        var key = $"{sessionId}:{filePath}";
        var now = DateTime.UtcNow;

        if (_recentChanges.TryGetValue(key, out var lastChange) && now - lastChange < _throttleWindow)
            return;

        _recentChanges[key] = now;

        FileChanged?.Invoke(this, new FileChangedEventArgs
        {
            SessionId = sessionId,
            FilePath = filePath,
            ChangeType = changeType
        });
    }

    private static bool ShouldIgnore(string filePath)
    {
        var normalized = filePath.Replace('\\', '/');
        return normalized.Contains("/.git/") ||
               normalized.Contains("/bin/") ||
               normalized.Contains("/obj/") ||
               normalized.Contains("/node_modules/") ||
               normalized.EndsWith(".tmp") ||
               normalized.EndsWith("~");
    }

    private void CleanupOldEntries(object? sender, ElapsedEventArgs e)
    {
        var cutoff = DateTime.UtcNow - TimeSpan.FromSeconds(10);
        var staleKeys = _recentChanges
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in staleKeys)
            _recentChanges.TryRemove(key, out _);
    }

    public void Dispose()
    {
        _cleanupTimer.Stop();
        _cleanupTimer.Dispose();

        foreach (var kvp in _watchers)
        {
            kvp.Value.EnableRaisingEvents = false;
            kvp.Value.Dispose();
        }
        _watchers.Clear();
        GC.SuppressFinalize(this);
    }
}
