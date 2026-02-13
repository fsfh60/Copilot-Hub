using CopilotHub.Core.Services;
using Microsoft.Toolkit.Uwp.Notifications;
using Serilog;

namespace CopilotHub.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger _logger = Log.ForContext<NotificationService>();

    public void ShowSessionCompleted(string sessionName, bool success)
    {
        try
        {
            var status = success ? "completed successfully" : "failed";
            new ToastContentBuilder()
                .AddText("CopilotHub Session Update")
                .AddText($"'{sessionName}' {status}")
                .Show();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to show toast notification");
        }
    }

    public void ShowFileChangesDetected(string sessionName, int fileCount)
    {
        try
        {
            new ToastContentBuilder()
                .AddText("File Changes Detected")
                .AddText($"{fileCount} file(s) changed in '{sessionName}'")
                .Show();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to show toast notification");
        }
    }
}
