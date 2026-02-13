using System.Collections.Concurrent;
using System.Diagnostics;
using CopilotHub.Core.Models;
using CopilotHub.Core.Services;
using Serilog;

namespace CopilotHub.Infrastructure.Services;

public class CopilotCliService : ICopilotProcessService
{
    private readonly ISessionManager _sessionManager;
    private readonly IDispatcherService _dispatcher;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _sessions = new();
    private readonly ConcurrentDictionary<Guid, Process> _activeProcesses = new();
    private readonly ILogger _logger = Log.ForContext<CopilotCliService>();

    public CopilotCliService(ISessionManager sessionManager, IDispatcherService dispatcher)
    {
        _sessionManager = sessionManager;
        _dispatcher = dispatcher;
    }

    public Task StartSessionAsync(CopilotSession session, CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _sessions[session.Id] = cts;

        _dispatcher.Invoke(() =>
        {
            session.OutputLog.Add("╔══════════════════════════════════════════╗");
            session.OutputLog.Add("║       CopilotHub Session Ready          ║");
            session.OutputLog.Add("╚══════════════════════════════════════════╝");
            session.OutputLog.Add("");
            session.OutputLog.Add($"  Model: {session.CopilotModel}");
            session.OutputLog.Add($"  Flags: {session.CopilotExtraArgs}");
            session.OutputLog.Add($"  Dir:   {session.WorkingDirectory}");
            session.OutputLog.Add("");
            session.OutputLog.Add("Type a prompt below and click Send (or press Enter).");
            session.OutputLog.Add("");
        });

        _logger.Information("Session {SessionId} initialized for {Dir}", session.Id, session.WorkingDirectory);
        return Task.CompletedTask;
    }

    public async Task SendInputAsync(Guid sessionId, string input, CancellationToken cancellationToken = default)
    {
        var session = _sessionManager.GetSession(sessionId);
        if (session is null) return;

        if (!_sessions.TryGetValue(sessionId, out var cts))
            return;

        var linkedCt = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token;

        _dispatcher.Invoke(() =>
        {
            session.OutputLog.Add($"▶ You: {input}");
            session.OutputLog.Add("");
            session.OutputLog.Add("⏳ Copilot is thinking...");
        });

        var psi = new ProcessStartInfo
        {
            FileName = "copilot",
            WorkingDirectory = session.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-p");
        psi.ArgumentList.Add(input);
        psi.ArgumentList.Add("--no-color");
        psi.ArgumentList.Add("-s");

        // Add model if specified
        if (!string.IsNullOrWhiteSpace(session.CopilotModel))
        {
            psi.ArgumentList.Add("--model");
            psi.ArgumentList.Add(session.CopilotModel);
        }

        // Add extra args (split by spaces, respecting quotes)
        if (!string.IsNullOrWhiteSpace(session.CopilotExtraArgs))
        {
            foreach (var arg in SplitArgs(session.CopilotExtraArgs))
                psi.ArgumentList.Add(arg);
        }

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        if (!_activeProcesses.TryAdd(sessionId, process))
        {
            _dispatcher.Invoke(() => session.OutputLog.Add("[WARN] A prompt is already running. Please wait."));
            return;
        }

        try
        {
            process.Start();
            _logger.Information("Copilot prompt started for session {SessionId}", sessionId);

            // Remove the "thinking" line
            _dispatcher.Invoke(() =>
            {
                if (session.OutputLog.Count > 0 && session.OutputLog[^1] == "⏳ Copilot is thinking...")
                    session.OutputLog.RemoveAt(session.OutputLog.Count - 1);
                session.OutputLog.Add("── Copilot ──────────────────────────────");
            });

            var stdoutTask = ReadStreamAsync(process.StandardOutput, session, isError: false, linkedCt);
            var stderrTask = ReadStreamAsync(process.StandardError, session, isError: true, linkedCt);

            await process.WaitForExitAsync(linkedCt);
            await Task.WhenAll(stdoutTask, stderrTask);

            _dispatcher.Invoke(() =>
            {
                session.OutputLog.Add("─────────────────────────────────────────");
                session.OutputLog.Add("");
            });

            if (process.ExitCode != 0)
            {
                _dispatcher.Invoke(() => session.OutputLog.Add($"[WARN] Copilot exited with code {process.ExitCode}"));
            }
        }
        catch (OperationCanceledException)
        {
            _dispatcher.Invoke(() => session.OutputLog.Add("[INFO] Prompt cancelled."));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error running Copilot prompt for session {SessionId}", sessionId);
            _dispatcher.Invoke(() => session.OutputLog.Add($"[ERROR] {ex.Message}"));
        }
        finally
        {
            _activeProcesses.TryRemove(sessionId, out _);
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); }
                catch { /* swallow */ }
            }
            process.Dispose();
        }
    }

    public void StopSession(Guid sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        if (_activeProcesses.TryRemove(sessionId, out var process) && !process.HasExited)
        {
            try { process.Kill(entireProcessTree: true); }
            catch (Exception ex) { _logger.Warning(ex, "Error killing process for session {SessionId}", sessionId); }
        }
    }

    private async Task ReadStreamAsync(
        System.IO.StreamReader reader, CopilotSession session, bool isError, CancellationToken ct)
    {
        try
        {
            var prefix = isError ? "[ERR] " : "";
            string? line;
            while (!ct.IsCancellationRequested && (line = await reader.ReadLineAsync(ct)) is not null)
            {
                var text = line;
                _dispatcher.Invoke(() => session.OutputLog.Add($"{prefix}{text}"));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error reading stream for session {SessionId}", session.Id);
        }
    }

    private static IEnumerable<string> SplitArgs(string args)
    {
        var current = new System.Text.StringBuilder();
        var inQuotes = false;
        foreach (var c in args)
        {
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0) { yield return current.ToString(); current.Clear(); }
                continue;
            }
            current.Append(c);
        }
        if (current.Length > 0) yield return current.ToString();
    }
}
