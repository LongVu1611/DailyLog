using System.IO;
using System.Windows.Threading;
using System.Windows;
using DailyLogAssistant.Data;
using DailyLogAssistant.Services;
using DailyLogAssistant.ViewModels;
using DailyLogAssistant.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DailyLogAssistant;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private SingleInstanceService? _singleInstance;

    public App() => DispatcherUnhandledException += HandleDispatcherException;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            _singleInstance = new SingleInstanceService();
            if (!_singleInstance.IsPrimary)
            {
                _singleInstance.SignalPrimary();
                Shutdown();
                return;
            }

            var dataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DailyLogAssistant");
            Directory.CreateDirectory(Path.Combine(dataFolder, "Logs"));
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(Path.Combine(dataFolder, "Logs", "application-.log"),
                    rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
                .CreateLogger();

            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices(services =>
                {
                    services.AddDbContextFactory<AppDbContext>(options =>
                        options.UseSqlite($"Data Source={Path.Combine(dataFolder, "DailyLogAssistant.db")}"));
                    services.AddSingleton<DatabaseInitializer>();
                    services.AddSingleton<LogService>();
                    services.AddSingleton<SettingsService>();
                    services.AddSingleton<ExportService>();
                    services.AddSingleton<StatisticsService>();
                    services.AddSingleton<AutoStartHelper>();
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<DailyReminderService>();
                    services.AddHostedService(provider => provider.GetRequiredService<DailyReminderService>());
                })
                .Build();

            await _host.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
            var window = _host.Services.GetRequiredService<MainWindow>();
            var viewModel = _host.Services.GetRequiredService<MainViewModel>();
            await viewModel.InitializeAsync();
            await _host.StartAsync();
            MainWindow = window;
            _singleInstance.ActivatePrimary += (_, _) =>
                Dispatcher.BeginInvoke(new Action(window.ShowDashboard));
            if (viewModel.Settings.LaunchLogAutomatically)
                window.ShowDailyLog();
            else
                window.Show();
            Log.Information("ApplicationStarted");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Application startup failed");
            System.Windows.MessageBox.Show($"Daily Log Assistant could not start.\n\n{exception.Message}",
                "Daily Log Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void HandleDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "UnhandledException");
        System.Windows.MessageBox.Show(
            $"An unexpected error occurred. Your saved logs are safe.\n\n{e.Exception.Message}",
            "Daily Log Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("ApplicationClosed");
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        _singleInstance?.Dispose();
        ShutdownMode = ShutdownMode.OnLastWindowClose;
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
