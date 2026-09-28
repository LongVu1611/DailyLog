using DailyLogAssistant.Models;

namespace DailyLogAssistant.Services;

public sealed class StatisticsService(LogService logs) : IStatisticsService
{
    public async Task<LogStatistics> GetAsync(CancellationToken cancellationToken = default)
    {
        var entries = await logs.SearchAsync(new LogQuery(), cancellationToken);
        var legacyEntries = await logs.GetAllAsync(cancellationToken: cancellationToken);
        var genericWorkDates = entries.Where(log => log.CategoryId == 1).Select(log => log.Date).ToHashSet();
        var legacy = legacyEntries.Where(item => !genericWorkDates.Contains(item.Date)).ToList();
        var workDates = entries.Where(log => log.CategoryId == 1).Select(log => log.Date).ToHashSet();
        foreach (var item in legacy) workDates.Add(item.Date);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var current = 0;
        var cursor = workDates.Contains(today) ? today : today.AddDays(-1);
        while (workDates.Contains(cursor)) { current++; cursor = cursor.AddDays(-1); }
        var longest = 0;
        var run = 0;
        DateOnly? previous = null;
        foreach (var date in workDates.Order())
        {
            run = previous is not null && date.DayNumber == previous.Value.DayNumber + 1 ? run + 1 : 1;
            longest = Math.Max(longest, run);
            previous = date;
        }
        var thisMonth = entries.Count(log => log.Date.Year == today.Year &&
            log.Date.Month == today.Month && log.Date <= today) +
            legacy.Count(log => log.Date.Year == today.Year && log.Date.Month == today.Month && log.Date <= today);
        var total = entries.Count + legacy.Count;
        var work = entries.Where(log => log.CategoryId == 1).ToList();
        var completed = work.Count(log => log.Status == "Completed");
        var rate = work.Count == 0 ? 0 : (int)Math.Round(completed * 100d / work.Count);
        return new LogStatistics(total, current, longest, thisMonth, rate);
    }

    public async Task<CategoryStatistics> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var entries = await logs.SearchAsync(new LogQuery(), cancellationToken);
        var counts = entries.GroupBy(log => log.Category?.Name ?? "Unknown")
            .ToDictionary(group => group.Key, group => group.Count());
        var work = entries.Where(log => log.CategoryId == 1).ToList();
        return new CategoryStatistics(counts, work.Count(log => log.Status == "Completed"),
            work.Count, entries.Count(log => log.Date.Year == DateTime.Today.Year));
    }
}

public sealed record LogStatistics(int Total, int CurrentStreak, int LongestStreak, int ThisMonth, int CompletionRate);
