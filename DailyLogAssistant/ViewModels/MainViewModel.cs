using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyLogAssistant.Helpers;
using DailyLogAssistant.Models;
using DailyLogAssistant.Services;
using Serilog;

namespace DailyLogAssistant.ViewModels;

public partial class MainViewModel(
    LogService logs,
    ITagService tagService,
    ICategoryService categoryService,
    SettingsService settingsService,
    StatisticsService statisticsService,
    AutoStartHelper autoStart,
    DailyReminderService reminderService,
    ThemeService themeService) : ObservableObject
{
    [ObservableProperty] private string activeSection = "Dashboard";
    [ObservableProperty] private DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Today);
    [ObservableProperty] private string title = "";
    [ObservableProperty] private string project = "";
    [ObservableProperty] private string status = "Completed";
    [ObservableProperty] private string recipient = "";
    [ObservableProperty] private string mood = "";
    [ObservableProperty] private string opening = "";
    [ObservableProperty] private string body = "";
    [ObservableProperty] private string closing = "";
    [ObservableProperty] private string signature = "";
    [ObservableProperty] private string content = "";
    [ObservableProperty] private string thingsToRemember = "";
    [ObservableProperty] private string result = "";
    [ObservableProperty] private string problems = "";
    [ObservableProperty] private string notes = "";
    [ObservableProperty] private string tagsText = "";
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private string historyFilter = "All time";
    [ObservableProperty] private string historyStatusFilter = "All statuses";
    [ObservableProperty] private string historyCategoryFilter = "All categories";
    [ObservableProperty] private string historyTagFilter = "All tags";
    [ObservableProperty] private string exportFormat = "Excel";
    [ObservableProperty] private string exportCategory = "WORK";
    [ObservableProperty] private string exportTagFilter = "All tags";
    [ObservableProperty] private string tagName = "";
    [ObservableProperty] private string tagColor = "#315C4C";
    [ObservableProperty] private string customCategoryName = "";
    [ObservableProperty] private string customCategoryIcon = "●";
    [ObservableProperty] private string customCategoryColor = "#315C4C";
    [ObservableProperty] private DateOnly historyStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
    [ObservableProperty] private DateOnly historyEndDate = DateOnly.FromDateTime(DateTime.Today);
    [ObservableProperty] private DateOnly exportStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-6));
    [ObservableProperty] private DateOnly exportEndDate = DateOnly.FromDateTime(DateTime.Today);
    [ObservableProperty] private string statusMessage = "Ready";
    [ObservableProperty] private LogEntry? selectedLog;
    [ObservableProperty] private Tag? selectedTag;
    [ObservableProperty] private LogCategory? selectedCategory;
    [ObservableProperty] private AppSettings settings = new();
    [ObservableProperty] private LogStatistics statistics = new(0, 0, 0, 0, 0);
    [ObservableProperty] private CategoryStatistics categoryStatistics = new(
        new Dictionary<string, int>(), 0, 0, 0);
    [ObservableProperty] private LogEntry? todayWorkLog;
    [ObservableProperty] private ObservableCollection<LogCategory> categories = [];
    [ObservableProperty] private ObservableCollection<Tag> tags = [];
    [ObservableProperty] private ObservableCollection<LogEntry> history = [];
    [ObservableProperty] private bool isReminderOpen;
    [ObservableProperty] private bool initialized;
    private int _editingId;

    public string TodayText => DateTime.Today.ToString("dddd, d MMMM yyyy");
    public string TodayStatus => TodayWorkLog is null ? "Work log pending" : "Work log completed";
    public string TodaySummary => TodayWorkLog?.DisplayContent ?? "No work entry yet. Record today's progress.";
    public string CategorySummary => string.Join("   •   ",
        CategoryStatistics.Counts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}: {pair.Value}"));
    public string WorkCompletionText => CategoryStatistics.WorkTotal == 0 ? "No work logs yet" :
        $"{CategoryStatistics.WorkCompleted}/{CategoryStatistics.WorkTotal} completed";
    public string ReminderTimeText => $"Work reminder at {Settings.ReminderTime}";
    public string[] HistoryFilters { get; } = ["All time", "Today", "This week", "This month", "Custom range"];
    public string[] StatusOptions { get; } = ["Planned", "In Progress", "Completed", "Blocked", "Cancelled"];
    public string[] ThemeOptions { get; } = ["System", "Light", "Dark"];
    public string[] HistoryCategoryFilters => ["All categories", .. Categories.Select(category => category.Name)];
    public string[] HistoryTagFilters => ["All tags", .. Tags.Select(tag => tag.Name)];
    public string[] AccentOptions { get; } = ["#315C4C", "#2878B5", "#7953A9", "#C46A28", "#B43E45"];
    public string[] ExportFormats { get; } = ["Excel", "CSV", "TXT", "Markdown"];
    public string[] ExportCategories { get; } = ["WORK", "PERSONAL", "LETTER", "NOTE", "All categories"];
    public event EventHandler? ReminderRequested;

    partial void OnSelectedTagChanged(Tag? value)
    {
        TagName = value?.Name ?? "";
        TagColor = value?.Color ?? "#315C4C";
    }

    partial void OnSearchTextChanged(string value)
    {
        if (Initialized) _ = RefreshHistorySafelyAsync();
    }

    partial void OnHistoryFilterChanged(string value)
    {
        if (Initialized) _ = RefreshHistorySafelyAsync();
    }

    partial void OnHistoryStatusFilterChanged(string value)
    {
        if (Initialized) _ = RefreshHistorySafelyAsync();
    }

    partial void OnHistoryCategoryFilterChanged(string value)
    {
        if (Initialized) _ = RefreshHistorySafelyAsync();
    }

    partial void OnHistoryTagFilterChanged(string value)
    {
        if (Initialized) _ = RefreshHistorySafelyAsync();
    }

    partial void OnExportTagFilterChanged(string value) => OnPropertyChanged(nameof(ExportTagFilter));

    partial void OnHistoryStartDateChanged(DateOnly value)
    {
        if (Initialized && HistoryFilter == "Custom range") _ = RefreshHistorySafelyAsync();
    }

    partial void OnHistoryEndDateChanged(DateOnly value)
    {
        if (Initialized && HistoryFilter == "Custom range") _ = RefreshHistorySafelyAsync();
    }

    partial void OnSettingsChanged(AppSettings value) => OnPropertyChanged(nameof(ReminderTimeText));

    public async Task InitializeAsync()
    {
        Settings = await settingsService.GetAsync();
        Categories = new ObservableCollection<LogCategory>(await categoryService.GetCategoriesAsync());
        Tags = new ObservableCollection<Tag>(await tagService.GetTagsAsync());
        OnPropertyChanged(nameof(HistoryCategoryFilters));
        OnPropertyChanged(nameof(HistoryTagFilters));
        SelectedCategory = Categories.FirstOrDefault(category => category.Name == "WORK");
        await RefreshHistoryAsync();
        await RefreshDashboardAsync();
        reminderService.ReminderDue += OnReminderDue;
        Initialized = true;
    }

    [RelayCommand]
    private async Task NavigateAsync(string? section)
    {
        if (string.IsNullOrWhiteSpace(section)) return;
        var category = Categories.FirstOrDefault(item => item.Name.Equals(section, StringComparison.OrdinalIgnoreCase));
        if (category is not null)
        {
            SelectedCategory = category;
            ActiveSection = "Editor";
            await LoadTodayForCategoryAsync(category);
            return;
        }
        ActiveSection = section;
    }

    [RelayCommand] private void NewWork() => StartNewLog("WORK");
    [RelayCommand] private void NewPersonal() => StartNewLog("PERSONAL");
    [RelayCommand] private void NewLetter() => StartNewLog("LETTER");
    [RelayCommand] private void NewNote() => StartNewLog("NOTE");

    [RelayCommand]
    private async Task OpenTodayWorkAsync() => await NavigateAsync("WORK");

    private void StartNewLog(string categoryName)
    {
        SelectedCategory = Categories.FirstOrDefault(item => item.Name == categoryName);
        _editingId = 0;
        SelectedDate = DateOnly.FromDateTime(DateTime.Today);
        Title = Project = Recipient = Mood = Opening = Body = Closing = Signature = "";
        Content = ThingsToRemember = Result = Problems = Notes = TagsText = "";
        Status = categoryName == "WORK" ? "Completed" : "Planned";
        IsReminderOpen = false;
        ActiveSection = "Editor";
    }

    [RelayCommand]
    private async Task SaveLogAsync()
    {
        if (SelectedCategory is null) return;
        try
        {
            await logs.SaveLogAsync(new LogEntry
            {
                Id = _editingId,
                CategoryId = SelectedCategory.Id,
                Date = SelectedDate,
                Title = Title.Trim(),
                Project = Project.Trim(),
                Status = Status,
                Recipient = Recipient.Trim(),
                Mood = Mood.Trim(),
                Opening = Opening,
                Body = Body,
                Closing = Closing,
                Signature = Signature,
                Content = Content,
                ThingsToRemember = ThingsToRemember,
                Result = Result,
                Problems = Problems,
                Notes = Notes,
                TagsText = TagsText
            });
            StatusMessage = "Log saved successfully.";
            IsReminderOpen = false;
            Log.Information("LogSaved for {Category} on {Date}", SelectedCategory.Name, SelectedDate);
            Tags = new ObservableCollection<Tag>(await tagService.GetTagsAsync());
            OnPropertyChanged(nameof(HistoryTagFilters));
            await RefreshHistoryAsync();
            await RefreshDashboardAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not save log");
            StatusMessage = "Could not save this log.";
            System.Windows.MessageBox.Show($"{StatusMessage}\n\n{ex.Message}", "Personal Log Manager",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task OpenSelectedLogAsync()
    {
        if (SelectedLog is null) return;
        _editingId = SelectedLog.Id;
        SelectedCategory = Categories.FirstOrDefault(category => category.Id == SelectedLog.CategoryId);
        SelectedDate = SelectedLog.Date;
        Title = SelectedLog.Title;
        Project = SelectedLog.Project;
        Status = SelectedLog.Status;
        Recipient = SelectedLog.Recipient;
        Mood = SelectedLog.Mood;
        Opening = SelectedLog.Opening;
        Body = SelectedLog.Body;
        Closing = SelectedLog.Closing;
        Signature = SelectedLog.Signature;
        Content = SelectedLog.Content;
        ThingsToRemember = SelectedLog.ThingsToRemember;
        Result = SelectedLog.Result;
        Problems = SelectedLog.Problems;
        Notes = SelectedLog.Notes;
        TagsText = string.Join(", ", SelectedLog.Tags);
        ActiveSection = "Editor";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task DeleteLogAsync()
    {
        if (SelectedLog is null) return;
        try
        {
            await logs.DeleteLogAsync(SelectedLog.Id);
            SelectedLog = null;
            StatusMessage = "Log deleted.";
            await RefreshHistoryAsync();
            await RefreshDashboardAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not delete log");
            System.Windows.MessageBox.Show(ex.Message, "Personal Log Manager",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DuplicateLogAsync()
    {
        if (SelectedLog is null) return;
        await OpenSelectedLogAsync();
        _editingId = 0;
        SelectedDate = DateOnly.FromDateTime(DateTime.Today);
        Title = $"{Title} (copy)";
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (!TimeOnly.TryParse(Settings.ReminderTime, out var reminderTime))
        {
            System.Windows.MessageBox.Show("Enter a valid time such as 17:00.", "Settings",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        try
        {
            Settings.ReminderTime = reminderTime.ToString("HH:mm");
            autoStart.SetEnabled(Settings.StartWithWindows);
            await settingsService.SaveAsync(Settings);
            themeService.Apply(Settings);
            OnPropertyChanged(nameof(ReminderTimeText));
            StatusMessage = "Settings saved.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not save settings");
            System.Windows.MessageBox.Show(ex.Message, "Settings",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task SnoozeAsync(string? minutes)
    {
        var duration = minutes switch { "15" => TimeSpan.FromMinutes(15), "60" => TimeSpan.FromHours(1), _ => TimeSpan.FromMinutes(30) };
        await settingsService.SnoozeAsync(duration);
        Settings = await settingsService.GetAsync();
        IsReminderOpen = false;
        StatusMessage = $"Work reminder snoozed for {(int)duration.TotalMinutes} minutes.";
    }

    [RelayCommand]
    private async Task DismissReminderAsync()
    {
        await settingsService.DismissTodayAsync();
        Settings = await settingsService.GetAsync();
        IsReminderOpen = false;
        StatusMessage = "Reminder dismissed for today.";
    }

    [RelayCommand]
    private async Task TestReminderAsync()
    {
        IsReminderOpen = true;
        await NavigateAsync("WORK");
        IsReminderOpen = true;
    }

    [RelayCommand]
    private async Task SaveTagAsync()
    {
        var tag = await tagService.SaveTagAsync(TagName, TagColor, SelectedTag?.Id);
        SelectedTag = tag;
        Tags = new ObservableCollection<Tag>(await tagService.GetTagsAsync());
        OnPropertyChanged(nameof(HistoryTagFilters));
        StatusMessage = "Tag saved.";
    }

    [RelayCommand]
    private async Task DeleteTagAsync()
    {
        if (SelectedTag is null) return;
        await tagService.DeleteTagAsync(SelectedTag.Id);
        SelectedTag = null;
        Tags = new ObservableCollection<Tag>(await tagService.GetTagsAsync());
        OnPropertyChanged(nameof(HistoryTagFilters));
        await RefreshHistoryAsync();
        StatusMessage = "Tag deleted.";
    }

    public async Task CreateCategoryAsync(string name, string icon, string color)
    {
        await categoryService.CreateCustomCategoryAsync(name, icon, color);
        Categories = new ObservableCollection<LogCategory>(await categoryService.GetCategoriesAsync());
        OnPropertyChanged(nameof(HistoryCategoryFilters));
        StatusMessage = "Custom category created.";
    }

    public async Task LoadTodayForCategoryAsync(LogCategory category)
    {
        var entry = (await logs.SearchAsync(new LogQuery(
            CategoryId: category.Id,
            From: DateOnly.FromDateTime(DateTime.Today),
            To: DateOnly.FromDateTime(DateTime.Today)))).FirstOrDefault();
        if (entry is null) StartNewLog(category.Name);
        else
        {
            SelectedLog = entry;
            await OpenSelectedLogAsync();
        }
    }

    public async Task RefreshHistoryAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly? from = null;
        DateOnly? to = null;
        switch (HistoryFilter)
        {
            case "Today": from = to = today; break;
            case "This week": from = DateTimeHelper.GetStartOfWeek(today); to = today; break;
            case "This month": from = new DateOnly(today.Year, today.Month, 1); to = today; break;
            case "Custom range": from = HistoryStartDate; to = HistoryEndDate; break;
        }
        var status = HistoryStatusFilter == "All statuses" ? null : HistoryStatusFilter;
        var categoryId = HistoryCategoryFilter == "All categories" ? null :
            Categories.FirstOrDefault(category => category.Name == HistoryCategoryFilter)?.Id;
        var tagId = HistoryTagFilter == "All tags" ? null :
            Tags.FirstOrDefault(tag => tag.Name == HistoryTagFilter)?.Id;
        History = new ObservableCollection<LogEntry>(await logs.SearchAsync(
            new LogQuery(SearchText, categoryId, tagId, from, to, status)));
        Statistics = await statisticsService.GetAsync();
        CategoryStatistics = await statisticsService.GetCategoriesAsync();
        OnPropertyChanged(nameof(CategorySummary));
        OnPropertyChanged(nameof(WorkCompletionText));
    }

    public async Task RefreshDashboardAsync()
    {
        TodayWorkLog = (await logs.SearchAsync(new LogQuery(
            CategoryId: Categories.FirstOrDefault(category => category.Name == "WORK")?.Id,
            From: DateOnly.FromDateTime(DateTime.Today),
            To: DateOnly.FromDateTime(DateTime.Today)))).FirstOrDefault();
        OnPropertyChanged(nameof(TodayStatus));
        OnPropertyChanged(nameof(TodaySummary));
        Statistics = await statisticsService.GetAsync();
        CategoryStatistics = await statisticsService.GetCategoriesAsync();
        OnPropertyChanged(nameof(CategorySummary));
        OnPropertyChanged(nameof(WorkCompletionText));
    }

    public LogQuery GetExportQuery()
    {
        var categoryId = ExportCategory == "All categories" ? null :
            Categories.FirstOrDefault(category => category.Name == ExportCategory)?.Id;
        var tagId = ExportTagFilter == "All tags" ? null :
            Tags.FirstOrDefault(tag => tag.Name == ExportTagFilter)?.Id;
        return new LogQuery(CategoryId: categoryId, TagId: tagId,
            From: ExportStartDate, To: ExportEndDate);
    }

    public Task<List<LogEntry>> GetLogsForExportAsync(LogQuery query) => logs.SearchAsync(query);

    public LogEntry CreateDraft() => new()
    {
        Id = _editingId, CategoryId = SelectedCategory?.Id ?? 1, Date = SelectedDate,
        Title = Title, Project = Project, Status = Status, Recipient = Recipient, Mood = Mood,
        Opening = Opening, Body = Body, Closing = Closing, Signature = Signature, Content = Content,
        ThingsToRemember = ThingsToRemember, Result = Result, Problems = Problems, Notes = Notes,
        TagsText = TagsText
    };

    private async Task LoadEntrySafelyAsync()
    {
        try { await RefreshHistoryAsync(); }
        catch (Exception ex) { Log.Error(ex, "Could not refresh log history"); StatusMessage = "Could not refresh history."; }
    }

    private async Task RefreshHistorySafelyAsync() => await LoadEntrySafelyAsync();

    private void OnReminderDue(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            IsReminderOpen = true;
            await NavigateAsync("WORK");
            ReminderRequested?.Invoke(this, EventArgs.Empty);
        });
    }
}
