using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyLogAssistant.Helpers;
using DailyLogAssistant.Models;
using DailyLogAssistant.Services;
using Serilog;

namespace DailyLogAssistant.ViewModels;

public partial class MainViewModel(
    LogService logs,
    SettingsService settingsService,
    StatisticsService statisticsService,
    AutoStartHelper autoStart,
    DailyReminderService reminderService) : ObservableObject
{
    [ObservableProperty] private string activeSection = "Dashboard";
    [ObservableProperty] private DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Today);
    [ObservableProperty] private string workDone = "";
    [ObservableProperty] private string problems = "";
    [ObservableProperty] private string tomorrowPlan = "";
    [ObservableProperty] private string notes = "";
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private string historyFilter = "All time";
    [ObservableProperty] private DateOnly historyStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
    [ObservableProperty] private DateOnly historyEndDate = DateOnly.FromDateTime(DateTime.Today);
    [ObservableProperty] private string statusMessage = "Ready";
    [ObservableProperty] private DailyLog? selectedLog;
    [ObservableProperty] private AppSettings settings = new();
    [ObservableProperty] private LogStatistics statistics = new(0, 0, 0, 0, 0);
    [ObservableProperty] private DailyLog? todayLog;
    [ObservableProperty] private List<DailyLog> history = [];
    [ObservableProperty] private bool isReminderOpen;
    [ObservableProperty] private bool initialized;

    public string TodayText => DateTime.Today.ToString("dddd, d MMMM yyyy");
    public string TodayStatus => TodayLog is null ? "Daily log pending" : "Daily log completed";
    public string TodaySummary => TodayLog?.WorkDone ?? "No entry yet. Capture a few notes before you wrap up.";
    public string ReminderTimeText => $"Reminder at {Settings.ReminderTime}";
    public string[] HistoryFilters { get; } = ["All time", "Today", "This week", "This month", "Custom range"];
    public event EventHandler? ReminderRequested;

    public async Task InitializeAsync()
    {
        Settings = await settingsService.GetAsync();
        await LoadEntryAsync(SelectedDate);
        await RefreshHistoryAsync();
        await RefreshDashboardAsync();
        reminderService.ReminderDue += OnReminderDue;
        Initialized = true;
        OnPropertyChanged(nameof(ReminderTimeText));
    }

    partial void OnSelectedDateChanged(DateOnly value)
    {
        if (Initialized) _ = LoadEntrySafelyAsync(value);
    }

    partial void OnSearchTextChanged(string value)
    {
        if (Initialized) _ = RefreshHistorySafelyAsync();
    }

    partial void OnHistoryFilterChanged(string value)
    {
        if (Initialized) _ = RefreshHistorySafelyAsync();
    }

    partial void OnHistoryStartDateChanged(DateOnly value)
    {
        if (Initialized && HistoryFilter == "Custom range") _ = RefreshHistorySafelyAsync();
    }

    partial void OnHistoryEndDateChanged(DateOnly value)
    {
        if (Initialized && HistoryFilter == "Custom range") _ = RefreshHistorySafelyAsync();
    }

    partial void OnSettingsChanged(AppSettings value)
    {
        OnPropertyChanged(nameof(ReminderTimeText));
    }

    [RelayCommand]
    private void Navigate(string? section) => ActiveSection = section ?? "Dashboard";

    [RelayCommand]
    private async Task SaveLogAsync()
    {
        try
        {
            await logs.SaveAsync(new DailyLog
            {
                Date = SelectedDate,
                WorkDone = WorkDone.Trim(),
                Problems = Problems.Trim(),
                TomorrowPlan = TomorrowPlan.Trim(),
                Notes = Notes.Trim()
            });
            StatusMessage = "Daily log saved successfully.";
            Log.Information("DailyLogCreatedOrUpdated for {Date}", SelectedDate);
            await RefreshDashboardAsync();
            await RefreshHistoryAsync();
            if (SelectedDate == DateOnly.FromDateTime(DateTime.Today))
                IsReminderOpen = false;
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            System.Windows.MessageBox.Show(ex.Message, "Daily Log", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not save daily log");
            StatusMessage = "Could not save your log. Please try again.";
            System.Windows.MessageBox.Show($"{StatusMessage}\n\n{ex.Message}", "Daily Log",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task OpenTodayAsync()
    {
        ActiveSection = "Daily Log";
        SelectedDate = DateOnly.FromDateTime(DateTime.Today);
        await LoadEntryAsync(SelectedDate);
    }

    [RelayCommand]
    private async Task OpenLogAsync(DailyLog? log)
    {
        if (log is null) return;
        ActiveSection = "Daily Log";
        SelectedDate = log.Date;
        await LoadEntryAsync(log.Date);
    }

    [RelayCommand]
    private async Task DeleteLogAsync()
    {
        if (SelectedLog is null) return;
        try
        {
            await logs.DeleteAsync(SelectedLog.Date);
            Log.Information("DailyLogDeleted for {Date}", SelectedLog.Date);
            StatusMessage = "Log deleted.";
            SelectedLog = null;
            await RefreshHistoryAsync();
            await RefreshDashboardAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not delete daily log");
            StatusMessage = "Could not delete this log.";
            System.Windows.MessageBox.Show($"{StatusMessage}\n\n{ex.Message}", "Daily Log",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (!TimeOnly.TryParse(Settings.ReminderTime, out var reminderTime))
        {
            System.Windows.MessageBox.Show("Enter a valid reminder time, for example 17:00.",
                "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            Settings.ReminderTime = reminderTime.ToString("HH:mm");
            autoStart.SetEnabled(Settings.StartWithWindows);
            await settingsService.SaveAsync(Settings);
            OnPropertyChanged(nameof(ReminderTimeText));
            StatusMessage = "Settings saved.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not save settings");
            StatusMessage = "Could not save settings.";
            System.Windows.MessageBox.Show($"{StatusMessage}\n\n{ex.Message}", "Settings",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task SnoozeAsync(string? minutes)
    {
        var duration = minutes switch
        {
            "15" => TimeSpan.FromMinutes(15),
            "60" => TimeSpan.FromHours(1),
            _ => TimeSpan.FromMinutes(30)
        };
        try
        {
            await settingsService.SnoozeAsync(duration);
            IsReminderOpen = false;
            StatusMessage = $"Reminder snoozed for {(int)duration.TotalMinutes} minutes.";
            Settings = await settingsService.GetAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not snooze daily reminder");
            StatusMessage = "Could not snooze the reminder.";
            System.Windows.MessageBox.Show($"{StatusMessage}\n\n{ex.Message}", "Daily Log",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DismissReminderAsync()
    {
        try
        {
            await settingsService.DismissTodayAsync();
            IsReminderOpen = false;
            StatusMessage = "Reminder dismissed for today.";
            Settings = await settingsService.GetAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not dismiss daily reminder");
            StatusMessage = "Could not dismiss the reminder.";
            System.Windows.MessageBox.Show($"{StatusMessage}\n\n{ex.Message}", "Daily Log",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task TestReminderAsync()
    {
        IsReminderOpen = true;
        ActiveSection = "Daily Log";
        await OpenTodayAsync();
        IsReminderOpen = true;
    }

    public async Task RefreshHistoryAsync()
    {
        var entries = await logs.GetAllAsync(SearchText);
        var today = DateOnly.FromDateTime(DateTime.Today);
        History = HistoryFilter switch
        {
            "Today" => entries.Where(log => log.Date == today).ToList(),
            "This week" => entries.Where(log => log.Date >= DateTimeHelper.GetStartOfWeek(today) &&
                log.Date <= today).ToList(),
            "This month" => entries.Where(log => log.Date.Year == today.Year &&
                log.Date.Month == today.Month).ToList(),
            "Custom range" when HistoryStartDate <= HistoryEndDate =>
                entries.Where(log => log.Date >= HistoryStartDate && log.Date <= HistoryEndDate).ToList(),
            "Custom range" => [],
            _ => entries
        };
        Statistics = await statisticsService.GetAsync();
    }

    public async Task RefreshDashboardAsync()
    {
        TodayLog = await logs.GetAsync(DateOnly.FromDateTime(DateTime.Today));
        OnPropertyChanged(nameof(TodayStatus));
        OnPropertyChanged(nameof(TodaySummary));
        Statistics = await statisticsService.GetAsync();
    }

    private async Task LoadEntryAsync(DateOnly date)
    {
        var log = await logs.GetAsync(date);
        WorkDone = log?.WorkDone ?? "";
        Problems = log?.Problems ?? "";
        TomorrowPlan = log?.TomorrowPlan ?? "";
        Notes = log?.Notes ?? "";
        StatusMessage = log is null ? "Write a few notes to get started." : "Existing log loaded.";
    }

    private async Task LoadEntrySafelyAsync(DateOnly date)
    {
        try { await LoadEntryAsync(date); }
        catch (Exception ex) { Log.Error(ex, "Could not load daily log"); StatusMessage = "Could not load this log."; }
    }

    private async Task RefreshHistorySafelyAsync()
    {
        try { await RefreshHistoryAsync(); }
        catch (Exception ex) { Log.Error(ex, "Could not search daily logs"); StatusMessage = "Search failed."; }
    }

    private void OnReminderDue(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            IsReminderOpen = true;
            ActiveSection = "Daily Log";
            _ = OpenTodayAsync();
            ReminderRequested?.Invoke(this, EventArgs.Empty);
        });
    }
}
