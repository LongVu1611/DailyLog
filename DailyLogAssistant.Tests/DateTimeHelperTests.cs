using DailyLogAssistant.Helpers;
using Xunit;

namespace DailyLogAssistant.Tests;

public sealed class DateTimeHelperTests
{
    [Fact]
    public void GetLocalDate_UsesTheProvidedLocalTimestamp()
    {
        var value = new DateTime(2026, 9, 28, 17, 0, 0);

        Assert.Equal(new DateOnly(2026, 9, 28), DateTimeHelper.GetLocalDate(value));
    }

    [Fact]
    public void HasReachedTime_IncludesTheExactReminderMinute()
    {
        Assert.False(DateTimeHelper.HasReachedTime(
            new DateTime(2026, 9, 28, 16, 59, 59), new TimeOnly(17, 0)));
        Assert.True(DateTimeHelper.HasReachedTime(
            new DateTime(2026, 9, 28, 17, 0, 0), new TimeOnly(17, 0)));
    }

    [Fact]
    public void GetStartOfWeek_UsesMondayAndHandlesYearBoundary()
    {
        Assert.Equal(new DateOnly(2026, 9, 28),
            DateTimeHelper.GetStartOfWeek(new DateOnly(2026, 9, 30)));
        Assert.Equal(new DateOnly(2025, 12, 29),
            DateTimeHelper.GetStartOfWeek(new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void BuildMonthGrid_AlignsMondayAndMarksSelectedDaysWithLogs()
    {
        var loggedDate = new DateOnly(2026, 9, 28);
        var selectedDate = new DateOnly(2026, 9, 15);
        var grid = CalendarHelper.BuildMonthGrid(new DateOnly(2026, 9, 1),
            new Dictionary<DateOnly, CalendarDayMark> { [loggedDate] = new(2, "#2878B5") },
            selectedDate);

        Assert.Equal(42, grid.Count);
        Assert.Equal(new DateOnly(2026, 8, 31), grid[0].Date);
        Assert.Equal(DayOfWeek.Monday, grid[0].Date.DayOfWeek);
        Assert.Equal(30, grid.Count(day => day.IsInDisplayedMonth));
        Assert.Equal(2, grid.Single(day => day.Date == loggedDate).LogCount);
        Assert.Equal("#2878B5", grid.Single(day => day.Date == loggedDate).MarkerColor);
        Assert.True(grid.Single(day => day.Date == selectedDate).IsSelected);
        Assert.False(grid[0].IsInDisplayedMonth);
    }

    [Fact]
    public void BuildMonthGrid_IncludesFebruaryLeapDay()
    {
        var grid = CalendarHelper.BuildMonthGrid(new DateOnly(2024, 2, 1),
            new Dictionary<DateOnly, CalendarDayMark>(),
            new DateOnly(2024, 2, 29));

        Assert.Equal(29, grid.Count(day => day.IsInDisplayedMonth));
        Assert.True(grid.Single(day => day.Date == new DateOnly(2024, 2, 29)).IsSelected);
    }
}
