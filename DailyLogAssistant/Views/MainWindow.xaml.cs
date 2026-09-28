using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DailyLogAssistant.Services;
using DailyLogAssistant.ViewModels;
using Microsoft.Win32;

namespace DailyLogAssistant.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ExportService _exports;
    private readonly System.Windows.Forms.NotifyIcon _trayIcon;
    private bool _exitRequested;

    public MainWindow(MainViewModel viewModel, ExportService exports)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _exports = exports;
        DataContext = viewModel;
        _viewModel.ReminderRequested += OnReminderRequested;
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Daily Log Assistant",
            Visible = true,
            ContextMenuStrip = CreateTrayMenu()
        };
        _trayIcon.DoubleClick += (_, _) => ShowDashboard();
        _trayIcon.BalloonTipClicked += (_, _) => ShowDailyLog();
        Microsoft.Win32.SystemEvents.SessionEnding += OnSessionEnding;
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized && _viewModel.Settings.MinimizeToTray)
                HideToTray();
        };
        Closing += OnClosing;
    }

    public void ShowDashboard()
    {
        Dispatcher.Invoke(() =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        });
    }

    public void ShowDailyLog()
    {
        ShowDashboard();
        _ = _viewModel.OpenTodayCommand.ExecuteAsync(null);
    }

    private System.Windows.Forms.ContextMenuStrip CreateTrayMenu()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Open Dashboard", null, (_, _) => ShowDashboard());
        menu.Items.Add("Write Today's Log", null, (_, _) => ShowDailyLog());
        menu.Items.Add("History", null, (_, _) => Dispatcher.Invoke(() =>
        {
            ShowDashboard();
            _viewModel.ActiveSection = "History";
        }));
        menu.Items.Add("Settings", null, (_, _) => Dispatcher.Invoke(() =>
        {
            ShowDashboard();
            _viewModel.ActiveSection = "Settings";
        }));
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Dispatcher.Invoke(() =>
        {
            _exitRequested = true;
            System.Windows.Application.Current.Shutdown();
        }));
        return menu;
    }

    private void HideToTray()
    {
        Hide();
        _trayIcon.ShowBalloonTip(1200, "Daily Log Assistant",
            "The app is still running in the notification area.", System.Windows.Forms.ToolTipIcon.Info);
    }

    private void OnReminderRequested(object? sender, EventArgs e)
    {
        ShowDashboard();
        if (_viewModel.Settings.ShowNotification)
            _trayIcon.ShowBalloonTip(8000, "Daily Log Reminder",
                "It is time to capture what you worked on today.", System.Windows.Forms.ToolTipIcon.Info);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_exitRequested && _viewModel.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _viewModel.ReminderRequested -= OnReminderRequested;
        Microsoft.Win32.SystemEvents.SessionEnding -= OnSessionEnding;
    }

    private void OnSessionEnding(object? sender, Microsoft.Win32.SessionEndingEventArgs e)
    {
        _exitRequested = true;
        Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
    }

    private async void History_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedLog is not null)
            await _viewModel.OpenLogCommand.ExecuteAsync(_viewModel.SelectedLog);
    }

    private async void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedLog is null) return;
        var result = System.Windows.MessageBox.Show($"Delete the log for {_viewModel.SelectedLog.Date:MMMM d, yyyy}?",
            "Delete daily log", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes) await _viewModel.DeleteLogCommand.ExecuteAsync(null);
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "daily-logs.csv" };
        if (dialog.ShowDialog() == true) await ExportAsync(() => _exports.ExportCsvAsync(dialog.FileName));
    }

    private async void ExportMarkdown_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Markdown files (*.md)|*.md", FileName = "daily-logs.md" };
        if (dialog.ShowDialog() == true) await ExportAsync(() => _exports.ExportMarkdownAsync(dialog.FileName));
    }

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "SQLite backup (*.db)|*.db",
            FileName = $"DailyLogAssistant-backup-{DateTime.Today:yyyyMMdd}.db"
        };
        if (dialog.ShowDialog() == true) await ExportAsync(() => _exports.BackupDatabaseAsync(dialog.FileName));
    }

    private static async Task ExportAsync(Func<Task> export)
    {
        try
        {
            await export();
            System.Windows.MessageBox.Show("Export completed successfully.", "Daily Log Assistant",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Export failed.\n\n{ex.Message}", "Daily Log Assistant",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
