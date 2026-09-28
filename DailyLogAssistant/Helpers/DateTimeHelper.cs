namespace DailyLogAssistant.Helpers;

public static class DateTimeHelper
{
    public static DateOnly GetLocalDate(DateTime? now = null) =>
        DateOnly.FromDateTime(now ?? DateTime.Now);

    public static bool HasReachedTime(DateTime now, TimeOnly time) =>
        TimeOnly.FromDateTime(now) >= time;

    public static DateOnly GetStartOfWeek(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday);
    }
}
