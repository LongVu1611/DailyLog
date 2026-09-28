using DailyLogAssistant.Services;
using Xunit;

namespace DailyLogAssistant.Tests;

public sealed class ReminderPolicyTests
{
    private static readonly DateOnly Day = new(2026, 9, 28);
    private static readonly TimeOnly ReminderTime = new(17, 0);

    [Fact]
    public void IsDue_AtReminderTime_ReturnsTrue()
    {
        Assert.True(ReminderPolicy.IsDue(Day.ToDateTime(new TimeOnly(17, 0)),
            ReminderTime, true, false, null, null, null));
    }

    [Fact]
    public void IsDue_BeforeReminderTime_ReturnsFalse()
    {
        Assert.False(ReminderPolicy.IsDue(Day.ToDateTime(new TimeOnly(16, 59)),
            ReminderTime, true, false, null, null, null));
    }

    [Fact]
    public void IsDue_WhenCompletedOrDisabled_ReturnsFalse()
    {
        var now = Day.ToDateTime(new TimeOnly(18, 0));
        Assert.False(ReminderPolicy.IsDue(now, ReminderTime, true, true, null, null, null));
        Assert.False(ReminderPolicy.IsDue(now, ReminderTime, false, false, null, null, null));
    }

    [Fact]
    public void IsDue_WhenDismissedForToday_ReturnsFalse()
    {
        Assert.False(ReminderPolicy.IsDue(Day.ToDateTime(new TimeOnly(18, 0)),
            ReminderTime, true, false, Day, null, null));
    }

    [Fact]
    public void IsDue_SnoozeWaitsUntilExpiration_ThenAllowsAnotherReminder()
    {
        var now = Day.ToDateTime(new TimeOnly(18, 0));
        Assert.False(ReminderPolicy.IsDue(now, ReminderTime, true, false, null, Day,
            now.AddMinutes(15)));
        Assert.True(ReminderPolicy.IsDue(now.AddMinutes(15), ReminderTime, true, false,
            null, Day, now.AddMinutes(15)));
    }

    [Fact]
    public void IsDue_ReminderDateDoesNotCarryAcrossMidnight()
    {
        var tomorrow = Day.AddDays(1).ToDateTime(new TimeOnly(17, 0));
        Assert.True(ReminderPolicy.IsDue(tomorrow, ReminderTime, true, false,
            null, Day, null));
    }

    [Fact]
    public void IsDue_DoesNotRepeatAfterReminderWasAlreadyShown()
    {
        Assert.False(ReminderPolicy.IsDue(Day.ToDateTime(new TimeOnly(18, 0)),
            ReminderTime, true, false, null, Day, null));
    }

    [Fact]
    public void IsDue_SnoozedReminderCanReturnAfterSnoozeExpires()
    {
        var now = Day.ToDateTime(new TimeOnly(18, 0));
        Assert.True(ReminderPolicy.IsDue(now, ReminderTime, true, false, null, Day,
            now.AddMinutes(-1)));
    }
}
