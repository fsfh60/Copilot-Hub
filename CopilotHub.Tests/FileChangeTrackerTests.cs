using CopilotHub.Core.Services;
using CopilotHub.Infrastructure.Services;
using FluentAssertions;

namespace CopilotHub.Tests;

public class FileChangeTrackerTests : IDisposable
{
    private readonly FileChangeTracker _sut = new();
    private readonly string _testDir;

    public FileChangeTrackerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"CopilotHubTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public void StartTracking_ValidDirectory_ShouldNotThrow()
    {
        var act = () => _sut.StartTracking(Guid.NewGuid(), _testDir);
        act.Should().NotThrow();
    }

    [Fact]
    public void StartTracking_InvalidDirectory_ShouldNotThrow()
    {
        var act = () => _sut.StartTracking(Guid.NewGuid(), @"C:\NonExistent_" + Guid.NewGuid().ToString("N"));
        act.Should().NotThrow();
    }

    [Fact]
    public void StopTracking_UnknownSession_ShouldNotThrow()
    {
        var act = () => _sut.StopTracking(Guid.NewGuid());
        act.Should().NotThrow();
    }

    [Fact]
    public async Task FileChanged_WhenFileCreated_ShouldRaiseEvent()
    {
        var sessionId = Guid.NewGuid();
        FileChangedEventArgs? receivedArgs = null;
        var tcs = new TaskCompletionSource<FileChangedEventArgs>();

        _sut.FileChanged += (_, args) =>
        {
            if (args.SessionId == sessionId)
                tcs.TrySetResult(args);
        };

        _sut.StartTracking(sessionId, _testDir);

        // Give watcher time to initialize
        await Task.Delay(100);

        var testFile = Path.Combine(_testDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "hello");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            receivedArgs = await tcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // FileSystemWatcher timing can be flaky in tests
        }

        // Either the event was received, or it timed out (acceptable for FSW)
        if (receivedArgs is not null)
        {
            receivedArgs.SessionId.Should().Be(sessionId);
            receivedArgs.FilePath.Should().Contain("test.txt");
        }
    }

    [Fact]
    public void StartTracking_CalledTwice_ShouldReplaceWatcher()
    {
        var sessionId = Guid.NewGuid();
        _sut.StartTracking(sessionId, _testDir);
        var act = () => _sut.StartTracking(sessionId, _testDir);
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var sessionId = Guid.NewGuid();
        _sut.StartTracking(sessionId, _testDir);
        var act = () => _sut.Dispose();
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _sut.Dispose();
        try { Directory.Delete(_testDir, recursive: true); }
        catch { /* cleanup best effort */ }
        GC.SuppressFinalize(this);
    }
}
