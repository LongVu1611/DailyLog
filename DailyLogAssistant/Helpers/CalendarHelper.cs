using DailyLogAssistant.Localization;

namespace DailyLogAssistant.Helpers;

public static class CalendarHelper
{
    public static IReadOnlyList<CalendarDay> BuildMonthGrid(
        DateOnly month,
        IReadOnlyDictionary<DateOnly, CalendarDayMark> logDays,
        DateOnly selectedDate)
    {
        var firstOfMonth = new DateOnly(month.Year, month.Month, 1);
        var gridStart = DateTimeHelper.GetStartOfWeek(firstOfMonth);
        return Enumerable.Range(0, 42)
            .Select(offset =>
            {
                var date = gridStart.AddDays(offset);
                var mark = logDays.GetValueOrDefault(date);
                return new CalendarDay(date, date.Year == month.Year && date.Month == month.Month, date == selectedDate,
                    mark?.Count ?? 0, mark?.Color ?? "#315C4C");
            })
            .ToArray();
    }
}

public sealed record CalendarDay(
    DateOnly Date,
    bool IsInDisplayedMonth,
    bool IsSelected,
    int LogCount,
    string MarkerColor)
{
    public int DayNumber => Date.Day;
    public string MarkerText => LogCount == 0 ? "" : $"● {LogCount}";
    public string ToolTip => Date.ToString("dddd, d MMMM yyyy", LocalizationService.Instance.CurrentCulture);
}

public sealed record CalendarDayMark(int Count, string Color);
