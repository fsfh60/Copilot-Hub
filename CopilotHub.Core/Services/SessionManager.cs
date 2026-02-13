using System.Collections.ObjectModel;
using CopilotHub.Core.Events;
using CopilotHub.Core.Models;

namespace CopilotHub.Core.Services;

public class SessionManager : ISessionManager
{
    private readonly ObservableCollection<CopilotSession> _sessions = [];

    public IReadOnlyList<CopilotSession> Sessions => _sessions;
    public ObservableCollection<CopilotSession> ObservableSessions => _sessions;

    public event EventHandler<SessionCompletedEventArgs>? SessionCompleted;

    public CopilotSession CreateSession(string workingDirectory, string name = "")
    {
        var session = new CopilotSession
        {
            WorkingDirectory = workingDirectory,
            Name = string.IsNullOrWhiteSpace(name)
                ? $"Session {_sessions.Count + 1}"
                : name
        };
        _sessions.Add(session);
        return session;
    }

    public void RemoveSession(Guid sessionId)
    {
        var session = GetSession(sessionId);
        if (session is not null)
            _sessions.Remove(session);
    }

    public CopilotSession? GetSession(Guid sessionId) =>
        _sessions.FirstOrDefault(s => s.Id == sessionId);

    public void CompleteSession(Guid sessionId, SessionStatus status)
    {
        var session = GetSession(sessionId);
        if (session is null) return;

        session.Status = status;
        SessionCompleted?.Invoke(this, new SessionCompletedEventArgs(session, status));
    }
}
