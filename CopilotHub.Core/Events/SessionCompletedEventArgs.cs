using CopilotHub.Core.Models;

namespace CopilotHub.Core.Events;

public class SessionCompletedEventArgs : EventArgs
{
    public CopilotSession Session { get; }
    public SessionStatus FinalStatus { get; }

    public SessionCompletedEventArgs(CopilotSession session, SessionStatus finalStatus)
    {
        Session = session;
        FinalStatus = finalStatus;
    }
}
