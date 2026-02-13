namespace CopilotHub.Core.Services;

public interface ITerminalService : IDisposable
{
    Task StartAsync(Guid sessionId, string workingDirectory, CancellationToken cancellationToken = default);
    Task SendCommandAsync(Guid sessionId, string command, CancellationToken cancellationToken = default);
    void Stop(Guid sessionId);
    event EventHandler<TerminalOutputEventArgs>? OutputReceived;
}

public class TerminalOutputEventArgs : EventArgs
{
    public Guid SessionId { get; init; }
    public string Text { get; init; } = string.Empty;
    public bool IsError { get; init; }
}
