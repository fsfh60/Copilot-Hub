using System.Windows;
using System.Windows.Threading;
using CopilotHub.Core.Services;

namespace CopilotHub.App.Services;

public class WpfDispatcherService : IDispatcherService
{
    public void Invoke(Action action)
    {
        if (Application.Current?.Dispatcher is Dispatcher dispatcher)
        {
            if (dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }

    public async Task InvokeAsync(Action action)
    {
        if (Application.Current?.Dispatcher is Dispatcher dispatcher)
        {
            if (dispatcher.CheckAccess())
                action();
            else
                await dispatcher.InvokeAsync(action);
        }
        else
        {
            action();
        }
    }
}
