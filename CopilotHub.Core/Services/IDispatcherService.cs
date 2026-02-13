namespace CopilotHub.Core.Services;

/// <summary>
/// Abstraction for dispatching actions to the UI thread.
/// </summary>
public interface IDispatcherService
{
    void Invoke(Action action);
    Task InvokeAsync(Action action);
}
