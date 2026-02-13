namespace CopilotHub.Core.Services;

public interface INotificationService
{
    void ShowSessionCompleted(string sessionName, bool success);
    void ShowFileChangesDetected(string sessionName, int fileCount);
}
