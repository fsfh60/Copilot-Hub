using CopilotHub.Core.Models;

namespace CopilotHub.Core.Services;

public interface ICopilotProcessService
{
    Task StartSessionAsync(CopilotSession session, CancellationToken cancellationToken = default);
    Task SendInputAsync(Guid sessionId, string input, CancellationToken cancellationToken = default);
    void StopSession(Guid sessionId);
}
