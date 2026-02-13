using System.Windows;
using System.Windows.Threading;
using CopilotHub.App.Services;
using CopilotHub.App.ViewModels;
using CopilotHub.Core.Services;
using CopilotHub.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CopilotHub.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/copilothub-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            var mainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
            mainWindow.Show();

            Log.Information("CopilotHub started successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to start CopilotHub");
            MessageBox.Show($"Failed to start:\n{ex}", "CopilotHub Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI exception");
        MessageBox.Show($"Unexpected error:\n{e.Exception.Message}", "CopilotHub Error",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            Log.Fatal(ex, "Unhandled domain exception");
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<IDispatcherService, WpfDispatcherService>();
        services.AddSingleton<ThemeService>();

        // Infrastructure (kept for file tracking, git, diff, notifications)
        services.AddSingleton<IFileChangeTracker, FileChangeTracker>();
        services.AddSingleton<IGitService, GitService>();
        services.AddSingleton<IDiffService, DiffService>();
        services.AddSingleton<INotificationService, NotificationService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Services is IDisposable disposable)
            disposable.Dispose();

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

