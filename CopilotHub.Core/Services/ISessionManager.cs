using CopilotHub.Core.Events;
using CopilotHub.Core.Models;

namespace CopilotHub.Core.Services;

public interface ISessionManager
{
    IReadOnlyList<CopilotSession> Sessions { get; }
    event EventHandler<SessionCompletedEventArgs>? SessionCompleted;

    CopilotSession CreateSession(string workingDirectory, string name = "");
    void RemoveSession(Guid sessionId);
    CopilotSession? GetSession(Guid sessionId);
    void CompleteSession(Guid sessionId, SessionStatus status);
}
