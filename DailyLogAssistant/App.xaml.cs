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
                "PersonalLogManager");
            Directory.CreateDirectory(Path.Combine(dataFolder, "Logs"));
            var databasePath = Path.Combine(dataFolder, "PersonalLogManager.db");
            await LegacyDatabaseMigrator.CopyDailyLogDatabaseAsync(databasePath);
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
                        options.UseSqlite($"Data Source={databasePath}"));
                    services.AddSingleton<DatabaseInitializer>();
                    services.AddSingleton<LogService>();
                    services.AddSingleton<ILogService>(provider => provider.GetRequiredService<LogService>());
                    services.AddSingleton<ITagService, TagService>();
                    services.AddSingleton<ICategoryService, CategoryService>();
                    services.AddSingleton<SettingsService>();
                    services.AddSingleton<ISettingsService>(provider => provider.GetRequiredService<SettingsService>());
                    services.AddSingleton<ExportService>();
                    services.AddSingleton<IExportService>(provider => provider.GetRequiredService<ExportService>());
                    services.AddSingleton<ILetterExportService, LetterExportService>();
                    services.AddSingleton<IExcelReportService, ExcelReportService>();
                    services.AddSingleton<INotificationService, NotificationService>();
                    services.AddSingleton<ThemeService>();
                    services.AddSingleton<StatisticsService>();
                    services.AddSingleton<IStatisticsService>(provider => provider.GetRequiredService<StatisticsService>());
                    services.AddSingleton<AutoStartHelper>();
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<DailyReminderService>();
                    services.AddSingleton<IReminderService>(provider => provider.GetRequiredService<DailyReminderService>());
                    services.AddHostedService(provider => provider.GetRequiredService<DailyReminderService>());
                })
                .Build();

            await _host.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
            var templateFolder = Path.Combine(dataFolder, "Templates");
            Directory.CreateDirectory(templateFolder);
            var templatePath = Path.Combine(templateFolder, "WorkReportTemplate.xlsx");
            if (!File.Exists(templatePath))
            {
                var bundledTemplate = Path.Combine(AppContext.BaseDirectory, "Resources", "WorkReportTemplate.xlsx");
                if (File.Exists(bundledTemplate)) File.Copy(bundledTemplate, templatePath);
                else ExcelReportService.CreateDefaultTemplate(templatePath);
            }
            var settingsService = _host.Services.GetRequiredService<SettingsService>();
            var appSettings = await settingsService.GetAsync();
            if (string.IsNullOrWhiteSpace(appSettings.WorkReportTemplatePath))
            {
                appSettings.WorkReportTemplatePath = templatePath;
                await settingsService.SaveAsync(appSettings);
            }
            _host.Services.GetRequiredService<AutoStartHelper>().SetEnabled(appSettings.StartWithWindows);
            var window = _host.Services.GetRequiredService<MainWindow>();
            var viewModel = _host.Services.GetRequiredService<MainViewModel>();
            await viewModel.InitializeAsync();
            _host.Services.GetRequiredService<ThemeService>().Apply(viewModel.Settings);
            await _host.StartAsync();
            MainWindow = window;
            _singleInstance.ActivatePrimary += (_, _) =>
                Dispatcher.BeginInvoke(new Action(window.ShowDashboard));
            if (appSettings.LaunchLogAutomatically)
                window.ShowDailyLog();
            else
                window.Show();
            Log.Information("PersonalLogManagerStarted");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Application startup failed");
            System.Windows.MessageBox.Show($"Personal Log Manager could not start.\n\n{exception.Message}",
                "Personal Log Manager", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void HandleDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "UnhandledException");
        System.Windows.MessageBox.Show(
            $"An unexpected error occurred. Your saved logs are safe.\n\n{e.Exception.Message}",
            "Personal Log Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("PersonalLogManagerClosed");
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
