using System.Collections.Concurrent;
using System.Diagnostics;
using CopilotHub.Core.Services;
using Serilog;

namespace CopilotHub.Infrastructure.Services;

public class TerminalService : ITerminalService
{
    private readonly IDispatcherService _dispatcher;
    private readonly ConcurrentDictionary<Guid, Process> _terminals = new();
    private readonly ILogger _logger = Log.ForContext<TerminalService>();

    public event EventHandler<TerminalOutputEventArgs>? OutputReceived;

    public TerminalService(IDispatcherService dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task StartAsync(Guid sessionId, string workingDirectory, CancellationToken cancellationToken = default)
    {
        Stop(sessionId);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoLogo -NoProfile -NoExit",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _terminals[sessionId] = process;

        process.Start();

        _ = ReadOutputAsync(sessionId, process.StandardOutput, isError: false, cancellationToken);
        _ = ReadOutputAsync(sessionId, process.StandardError, isError: true, cancellationToken);

        _logger.Information("Terminal started for session {SessionId} in {Dir}", sessionId, workingDirectory);
        await Task.CompletedTask;
    }

    public async Task SendCommandAsync(Guid sessionId, string command, CancellationToken cancellationToken = default)
    {
        _logger.Debug("SendCommandAsync called for session {SessionId}: {Command}", sessionId, command);
        
        if (_terminals.TryGetValue(sessionId, out var process))
        {
            if (process.HasExited)
            {
                _logger.Warning("Terminal process has exited for session {SessionId}", sessionId);
                return;
            }
            
            await process.StandardInput.WriteLineAsync(command.AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync(cancellationToken);
            _logger.Debug("Command written to terminal stdin for session {SessionId}", sessionId);
        }
        else
        {
            _logger.Warning("No terminal process found for session {SessionId}", sessionId);
        }
    }

    public void Stop(Guid sessionId)
    {
        if (_terminals.TryRemove(sessionId, out var process))
        {
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (Exception ex) { _logger.Warning(ex, "Error killing terminal for session {SessionId}", sessionId); }
            }
            process.Dispose();
        }
    }

    private async Task ReadOutputAsync(Guid sessionId, System.IO.StreamReader reader, bool isError, CancellationToken ct)
    {
        try
        {
            var buffer = new char[4096];
            while (!ct.IsCancellationRequested)
            {
                var bytesRead = await reader.ReadAsync(buffer.AsMemory(), ct);
                if (bytesRead == 0) break;

                var text = new string(buffer, 0, bytesRead);
                _dispatcher.Invoke(() =>
                {
                    OutputReceived?.Invoke(this, new TerminalOutputEventArgs
                    {
                        SessionId = sessionId,
                        Text = text,
                        IsError = isError
                    });
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _logger.Error(ex, "Error reading terminal output for session {SessionId}", sessionId); }
    }

    public void Dispose()
    {
        foreach (var kvp in _terminals)
        {
            if (!kvp.Value.HasExited)
            {
                try { kvp.Value.Kill(entireProcessTree: true); }
                catch { /* swallow */ }
            }
            kvp.Value.Dispose();
        }
        _terminals.Clear();
        GC.SuppressFinalize(this);
    }
}
