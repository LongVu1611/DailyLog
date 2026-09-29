using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyLogAssistant.Helpers;
using DailyLogAssistant.Localization;
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
    [ObservableProperty] private DateOnly calendarMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateOnly selectedCalendarDate = DateOnly.FromDateTime(DateTime.Today);
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
    [ObservableProperty] private LogCategory? quickAddCategory;
    [ObservableProperty] private string quickAddTitle = "";
    [ObservableProperty] private string quickAddContent = "";
    [ObservableProperty] private string quickAddStatus = "Completed";
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
    [ObservableProperty] private ObservableCollection<LogEntry> recentLogs = [];
    [ObservableProperty] private ObservableCollection<CalendarDay> calendarDays = [];
    [ObservableProperty] private ObservableCollection<LogEntry> calendarEntries = [];
    [ObservableProperty] private int todayLogCount;
    [ObservableProperty] private int weeklyWorkCompleted;
    [ObservableProperty] private int weeklyWorkTotal;
    [ObservableProperty] private bool isReminderOpen;
    [ObservableProperty] private bool initialized;
    private int _editingId;
    private List<LogEntry> _calendarMonthLogs = [];

    public string TodayText => DateTime.Today.ToString("dddd, d MMMM yyyy", LocalizationService.Instance.CurrentCulture);
    public string CalendarMonthText => CalendarMonth.ToString("MMMM yyyy", LocalizationService.Instance.CurrentCulture);
    public string SelectedCalendarDateText => SelectedCalendarDate.ToString("dddd, d MMMM yyyy", LocalizationService.Instance.CurrentCulture);
    public string WeeklyWorkSummary => WeeklyWorkTotal == 0
        ? LocalizationService.Translate("No work logs this week")
        : string.Format(LocalizationService.Instance.CurrentCulture,
            LocalizationService.Translate("{0} of {1} completed"), WeeklyWorkCompleted, WeeklyWorkTotal);
    public string TodayStatus => LocalizationService.Translate(
        TodayWorkLog is null ? "Work log pending" : "Work log completed");
    public string TodaySummary => TodayWorkLog?.DisplayContent ??
        LocalizationService.Translate("No work entry yet. Record today's progress.");
    public string CategorySummary => string.Join("   •   ",
        CategoryStatistics.Counts.OrderBy(pair => pair.Key)
            .Select(pair => $"{LocalizationService.Translate(pair.Key)}: {pair.Value}"));
    public string WorkCompletionText => CategoryStatistics.WorkTotal == 0
        ? LocalizationService.Translate("No work logs yet")
        : $"{CategoryStatistics.WorkCompleted}/{CategoryStatistics.WorkTotal} {LocalizationService.Translate("completed")}";
    public string ReminderTimeText => string.Format(LocalizationService.Instance.CurrentCulture,
        LocalizationService.Translate("Work reminder at {0}"), Settings.ReminderTime);
    public string TotalLogsText => string.Format(LocalizationService.Instance.CurrentCulture,
        LocalizationService.Translate("Total logs: {0}"), Statistics.Total);
    public string CurrentStreakText => string.Format(LocalizationService.Instance.CurrentCulture,
        LocalizationService.Translate("Current work streak: {0} days"), Statistics.CurrentStreak);
    public string LongestStreakText => string.Format(LocalizationService.Instance.CurrentCulture,
        LocalizationService.Translate("Longest streak: {0} days"), Statistics.LongestStreak);
    public string ThisMonthLogsText => string.Format(LocalizationService.Instance.CurrentCulture,
        LocalizationService.Translate("Logs this month: {0}"), Statistics.ThisMonth);
    public string WorkCompletionRateText => string.Format(LocalizationService.Instance.CurrentCulture,
        LocalizationService.Translate("Work completion rate: {0}%"), Statistics.CompletionRate);
    public string ThisYearLogsText => string.Format(LocalizationService.Instance.CurrentCulture,
        LocalizationService.Translate("Logs this year: {0}"), CategoryStatistics.ThisYear);
    public string[] HistoryFilters { get; } = ["All time", "Today", "This week", "This month", "Custom range"];
    public string[] StatusOptions { get; } = ["Planned", "In Progress", "Completed", "Blocked", "Cancelled"];
    public string[] HistoryStatusOptions { get; } = ["All statuses", "Planned", "In Progress", "Completed", "Blocked", "Cancelled"];
    public string[] ThemeOptions { get; } = ["System", "Light", "Dark"];
    public string[] LanguageOptions { get; } = ["English", "Tiếng Việt", "Русский"];
    public string[] HistoryCategoryFilters => ["All categories", .. Categories.Select(category => category.Name)];
    public string[] HistoryTagFilters => ["All tags", .. Tags.Select(tag => tag.Name)];
    public string[] AccentOptions { get; } = ["#315C4C", "#2878B5", "#7953A9", "#C46A28", "#B43E45"];
    public string[] ExportFormats { get; } = ["Excel", "CSV", "TXT", "Markdown"];
    public string[] ExportCategories { get; } = ["WORK", "PERSONAL", "LETTER", "NOTE", "All categories"];
    public event EventHandler? ReminderRequested;
    public event EventHandler? QuickAddSaved;

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var selectedCategoryId = SelectedCategory?.Id;
        var quickAddCategoryId = QuickAddCategory?.Id;
        Categories = new ObservableCollection<LogCategory>(Categories);
        SelectedCategory = Categories.FirstOrDefault(category => category.Id == selectedCategoryId);
        QuickAddCategory = Categories.FirstOrDefault(category => category.Id == quickAddCategoryId);
        OnPropertyChanged(nameof(TodayText));
        OnPropertyChanged(nameof(CalendarMonthText));
        OnPropertyChanged(nameof(SelectedCalendarDateText));
        OnPropertyChanged(nameof(WeeklyWorkSummary));
        OnPropertyChanged(nameof(TodayStatus));
        OnPropertyChanged(nameof(TodaySummary));
        OnPropertyChanged(nameof(CategorySummary));
        OnPropertyChanged(nameof(WorkCompletionText));
        OnPropertyChanged(nameof(ReminderTimeText));
        OnPropertyChanged(nameof(TotalLogsText));
        OnPropertyChanged(nameof(CurrentStreakText));
        OnPropertyChanged(nameof(LongestStreakText));
        OnPropertyChanged(nameof(ThisMonthLogsText));
        OnPropertyChanged(nameof(WorkCompletionRateText));
        OnPropertyChanged(nameof(ThisYearLogsText));
        OnPropertyChanged(nameof(HistoryFilters));
        OnPropertyChanged(nameof(StatusOptions));
        OnPropertyChanged(nameof(ThemeOptions));
        OnPropertyChanged(nameof(HistoryCategoryFilters));
        OnPropertyChanged(nameof(HistoryTagFilters));
        OnPropertyChanged(nameof(HistoryStatusOptions));
        OnPropertyChanged(nameof(ExportFormats));
        OnPropertyChanged(nameof(ExportCategories));
        StatusMessage = LocalizationService.Translate("Ready");
        UpdateCalendarSelection();
        if (Initialized) _ = RefreshLocalizedViewsSafelyAsync();
    }

    private async Task RefreshLocalizedViewsSafelyAsync()
    {
        SelectedLog = null;
        try
        {
            await RefreshHistoryAsync();
            await RefreshDashboardAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not refresh localized views");
            StatusMessage = LocalizationService.Translate("Could not refresh history.");
        }
    }

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

    partial void OnStatisticsChanged(LogStatistics value)
    {
        OnPropertyChanged(nameof(TotalLogsText));
        OnPropertyChanged(nameof(CurrentStreakText));
        OnPropertyChanged(nameof(LongestStreakText));
        OnPropertyChanged(nameof(ThisMonthLogsText));
        OnPropertyChanged(nameof(WorkCompletionRateText));
    }

    partial void OnCategoryStatisticsChanged(CategoryStatistics value) =>
        OnPropertyChanged(nameof(ThisYearLogsText));

    public async Task InitializeAsync()
    {
        Settings = await settingsService.GetAsync();
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        LocalizationService.SetLanguage(Settings.Language);
        Categories = new ObservableCollection<LogCategory>(await categoryService.GetCategoriesAsync());
        Tags = new ObservableCollection<Tag>(await tagService.GetTagsAsync());
        OnPropertyChanged(nameof(HistoryCategoryFilters));
        OnPropertyChanged(nameof(HistoryTagFilters));
        OnPropertyChanged(nameof(HistoryStatusOptions));
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
        if (section == "Calendar") await RefreshCalendarAsync();
    }

    [RelayCommand] private void NewWork() => StartNewLog("WORK");
    [RelayCommand] private void NewPersonal() => StartNewLog("PERSONAL");
    [RelayCommand] private void NewLetter() => StartNewLog("LETTER");
    [RelayCommand] private void NewNote() => StartNewLog("NOTE");

    [RelayCommand]
    private async Task OpenTodayWorkAsync() => await NavigateAsync("WORK");

    [RelayCommand]
    private async Task PreviousCalendarMonthAsync() => await ChangeCalendarMonthAsync(-1);

    [RelayCommand]
    private async Task NextCalendarMonthAsync() => await ChangeCalendarMonthAsync(1);

    [RelayCommand]
    private async Task CurrentCalendarMonthAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        CalendarMonth = new DateOnly(today.Year, today.Month, 1);
        SelectedCalendarDate = today;
        SelectedLog = null;
        OnPropertyChanged(nameof(CalendarMonthText));
        OnPropertyChanged(nameof(SelectedCalendarDateText));
        await RefreshCalendarAsync();
    }

    [RelayCommand]
    private void SelectCalendarDay(CalendarDay? day)
    {
        if (day is null || !day.IsInDisplayedMonth) return;
        SelectedCalendarDate = day.Date;
        SelectedLog = null;
        OnPropertyChanged(nameof(SelectedCalendarDateText));
        UpdateCalendarSelection();
    }

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
        await SaveDraftAsync(new LogEntry
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
    }

    private async Task SaveDraftAsync(LogEntry draft, bool isQuickAdd = false)
    {
        try
        {
            await logs.SaveLogAsync(draft);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not save log");
            StatusMessage = LocalizationService.Translate("Could not save this log.");
            System.Windows.MessageBox.Show($"{StatusMessage}\n\n{LocalizationService.TranslateException(ex)}",
                LocalizationService.Translate("Personal Log Manager"),
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        StatusMessage = LocalizationService.Translate("Log saved successfully.");
        IsReminderOpen = false;
        Log.Information("LogSaved for {CategoryId} on {Date}", draft.CategoryId, draft.Date);
        if (isQuickAdd)
        {
            QuickAddTitle = QuickAddContent = "";
            QuickAddSaved?.Invoke(this, EventArgs.Empty);
        }

        try
        {
            Tags = new ObservableCollection<Tag>(await tagService.GetTagsAsync());
            OnPropertyChanged(nameof(HistoryTagFilters));
            await RefreshHistoryAsync();
            await RefreshDashboardAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not refresh the application after saving a log");
            StatusMessage = LocalizationService.Translate("Log saved, but the views could not be refreshed.");
            System.Windows.MessageBox.Show($"{StatusMessage}\n\n{LocalizationService.TranslateException(ex)}",
                LocalizationService.Translate("Personal Log Manager"),
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    public void PrepareQuickAdd()
    {
        QuickAddCategory = Categories.FirstOrDefault(category => category.Name == "WORK")
            ?? Categories.FirstOrDefault();
        QuickAddTitle = "";
        QuickAddContent = "";
        QuickAddStatus = "Completed";
    }

    [RelayCommand]
    private async Task SaveQuickAddAsync()
    {
        if (QuickAddCategory is null)
        {
            System.Windows.MessageBox.Show(LocalizationService.Translate("Select a log type."),
                LocalizationService.Translate("Quick add"),
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var isLetter = QuickAddCategory.Name == "LETTER";
        await SaveDraftAsync(new LogEntry
        {
            CategoryId = QuickAddCategory.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Title = QuickAddTitle.Trim(),
            Status = QuickAddStatus,
            Body = isLetter ? QuickAddContent : "",
            Content = isLetter ? "" : QuickAddContent
        }, isQuickAdd: true);
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
            StatusMessage = LocalizationService.Translate("Log deleted.");
            await RefreshHistoryAsync();
            await RefreshDashboardAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not delete log");
            System.Windows.MessageBox.Show(LocalizationService.TranslateException(ex),
                LocalizationService.Translate("Personal Log Manager"),
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
            System.Windows.MessageBox.Show(LocalizationService.Translate("Enter a valid time such as 17:00."),
                LocalizationService.Translate("Settings"),
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        try
        {
            Settings.ReminderTime = reminderTime.ToString("HH:mm");
            autoStart.SetEnabled(Settings.StartWithWindows);
            await settingsService.SaveAsync(Settings);
            LocalizationService.SetLanguage(Settings.Language);
            themeService.Apply(Settings);
            OnPropertyChanged(nameof(ReminderTimeText));
            StatusMessage = LocalizationService.Translate("Settings saved.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not save settings");
            System.Windows.MessageBox.Show(LocalizationService.TranslateException(ex),
                LocalizationService.Translate("Settings"),
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
        StatusMessage = string.Format(LocalizationService.Instance.CurrentCulture,
            LocalizationService.Translate("Work reminder snoozed for {0} minutes."),
            (int)duration.TotalMinutes);
    }

    [RelayCommand]
    private async Task DismissReminderAsync()
    {
        await settingsService.DismissTodayAsync();
        Settings = await settingsService.GetAsync();
        IsReminderOpen = false;
        StatusMessage = LocalizationService.Translate("Reminder dismissed for today.");
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
        StatusMessage = LocalizationService.Translate("Tag saved.");
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
        StatusMessage = LocalizationService.Translate("Tag deleted.");
    }

    public async Task CreateCategoryAsync(string name, string icon, string color)
    {
        await categoryService.CreateCustomCategoryAsync(name, icon, color);
        Categories = new ObservableCollection<LogCategory>(await categoryService.GetCategoriesAsync());
        OnPropertyChanged(nameof(HistoryCategoryFilters));
        StatusMessage = LocalizationService.Translate("Custom category created.");
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
        var today = DateOnly.FromDateTime(DateTime.Today);
        SelectedLog = null;
        var todayEntries = await logs.SearchAsync(new LogQuery(From: today, To: today));
        TodayLogCount = todayEntries.Count;
        var workCategoryId = Categories.FirstOrDefault(category => category.Name == "WORK")?.Id;
        TodayWorkLog = todayEntries.FirstOrDefault(entry => entry.CategoryId == workCategoryId);
        OnPropertyChanged(nameof(TodayStatus));
        OnPropertyChanged(nameof(TodaySummary));
        var weekEntries = workCategoryId is null
            ? []
            : await logs.SearchAsync(new LogQuery(CategoryId: workCategoryId,
                From: DateTimeHelper.GetStartOfWeek(today), To: today));
        WeeklyWorkTotal = weekEntries.Count;
        WeeklyWorkCompleted = weekEntries.Count(entry => entry.Status == "Completed");
        OnPropertyChanged(nameof(WeeklyWorkSummary));
        RecentLogs = new ObservableCollection<LogEntry>(await logs.GetRecentAsync(5));
        Statistics = await statisticsService.GetAsync();
        CategoryStatistics = await statisticsService.GetCategoriesAsync();
        OnPropertyChanged(nameof(CategorySummary));
        OnPropertyChanged(nameof(WorkCompletionText));
        await RefreshCalendarAsync();
    }

    public async Task RefreshCalendarAsync()
    {
        SelectedLog = null;
        var monthStart = new DateOnly(CalendarMonth.Year, CalendarMonth.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        _calendarMonthLogs = await logs.SearchAsync(new LogQuery(From: monthStart, To: monthEnd));
        UpdateCalendarSelection();
    }

    private async Task ChangeCalendarMonthAsync(int months)
    {
        CalendarMonth = new DateOnly(CalendarMonth.Year, CalendarMonth.Month, 1).AddMonths(months);
        SelectedCalendarDate = CalendarMonth;
        SelectedLog = null;
        OnPropertyChanged(nameof(CalendarMonthText));
        OnPropertyChanged(nameof(SelectedCalendarDateText));
        await RefreshCalendarAsync();
    }

    private void UpdateCalendarSelection()
    {
        var marks = _calendarMonthLogs.GroupBy(entry => entry.Date)
            .ToDictionary(group => group.Key, group => new CalendarDayMark(
                group.Count(),
                group.Select(entry => entry.Category?.Color)
                    .FirstOrDefault(color => !string.IsNullOrWhiteSpace(color)) ?? "#315C4C"));
        CalendarDays = new ObservableCollection<CalendarDay>(
            CalendarHelper.BuildMonthGrid(CalendarMonth, marks, SelectedCalendarDate));
        CalendarEntries = new ObservableCollection<LogEntry>(
            _calendarMonthLogs.Where(entry => entry.Date == SelectedCalendarDate));
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
        catch (Exception ex)
        {
            Log.Error(ex, "Could not refresh log history");
            StatusMessage = LocalizationService.Translate("Could not refresh history.");
        }
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
