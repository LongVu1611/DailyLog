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
}
