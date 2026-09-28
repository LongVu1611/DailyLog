using DailyLogAssistant.Models;

namespace DailyLogAssistant.Services;

public interface ILogService
{
    Task<List<LogEntry>> SearchAsync(LogQuery query, CancellationToken cancellationToken = default);
    Task<LogEntry?> GetLogAsync(int id, CancellationToken cancellationToken = default);
    Task SaveLogAsync(LogEntry entry, CancellationToken cancellationToken = default);
    Task DeleteLogAsync(int id, CancellationToken cancellationToken = default);
}

public interface ITagService
{
    Task<List<Tag>> GetTagsAsync(CancellationToken cancellationToken = default);
    Task<Tag> SaveTagAsync(string name, string color, int? id = null, CancellationToken cancellationToken = default);
    Task DeleteTagAsync(int id, CancellationToken cancellationToken = default);
}

public interface ICategoryService
{
    Task<List<LogCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<LogCategory> CreateCustomCategoryAsync(string name, string icon, string color, CancellationToken cancellationToken = default);
    Task DeleteCustomCategoryAsync(int id, CancellationToken cancellationToken = default);
}

public interface IExportService
{
    Task ExportCsvAsync(string path, CancellationToken cancellationToken = default);
    Task ExportMarkdownAsync(string path, CancellationToken cancellationToken = default);
    Task ExportDatabaseAsync(string path, CancellationToken cancellationToken = default);
}

public interface IExcelReportService
{
    Task ExportWeeklyReportAsync(DateOnly from, DateOnly to, string path, CancellationToken cancellationToken = default);
}

public interface ILetterExportService
{
    Task ExportTxtAsync(LogEntry entry, string path, CancellationToken cancellationToken = default);
    Task ExportMarkdownAsync(LogEntry entry, string path, CancellationToken cancellationToken = default);
}

public interface IReminderService
{
    event EventHandler? ReminderDue;
}

public interface INotificationService
{
    void Show(string title, string message);
}

public interface ISettingsService
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
    Task SnoozeAsync(TimeSpan duration, CancellationToken cancellationToken = default);
    Task DismissTodayAsync(CancellationToken cancellationToken = default);
}

public interface IStatisticsService
{
    Task<LogStatistics> GetAsync(CancellationToken cancellationToken = default);
    Task<CategoryStatistics> GetCategoriesAsync(CancellationToken cancellationToken = default);
}

public sealed record LogQuery(
    string? Search = null,
    int? CategoryId = null,
    int? TagId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? Status = null);

public sealed record CategoryStatistics(IReadOnlyDictionary<string, int> Counts, int WorkCompleted, int WorkTotal, int ThisYear);

public sealed record TemplateSection(string Header, int HeaderRow, int FirstDataRow);

public sealed record ColumnMapping(int Number, int Date, int Task, int Result, int Notes, int Problem);
