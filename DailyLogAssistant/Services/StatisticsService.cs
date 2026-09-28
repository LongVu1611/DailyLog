using DailyLogAssistant.Models;

namespace DailyLogAssistant.Services;

public sealed class StatisticsService(LogService logs)
{
    public async Task<LogStatistics> GetAsync(CancellationToken cancellationToken = default)
    {
        var entries = await logs.GetAllAsync(cancellationToken: cancellationToken);
        var dates = entries.Select(log => log.Date).ToHashSet();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var current = 0;
        var cursor = dates.Contains(today) ? today : today.AddDays(-1);
        while (dates.Contains(cursor)) { current++; cursor = cursor.AddDays(-1); }
        var longest = 0;
        var run = 0;
        DateOnly? previous = null;
        foreach (var date in dates.Order())
        {
            run = previous is not null && date.DayNumber == previous.Value.DayNumber + 1 ? run + 1 : 1;
            longest = Math.Max(longest, run);
            previous = date;
        }
        var monthCount = today.Day;
        var completed = entries.Count(log => log.Date.Year == today.Year &&
            log.Date.Month == today.Month && log.Date <= today);
        return new LogStatistics(entries.Count, current, longest, completed,
            Math.Min(100, (int)Math.Round(completed * 100d / monthCount)));
    }
}

public sealed record LogStatistics(int Total, int CurrentStreak, int LongestStreak, int ThisMonth, int CompletionRate);
