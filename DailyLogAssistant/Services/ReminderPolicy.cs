using DailyLogAssistant.Helpers;

namespace DailyLogAssistant.Services;

public static class ReminderPolicy
{
    public static bool IsDue(
        DateTime now,
        TimeOnly reminderTime,
        bool enabled,
        bool hasLog,
        DateOnly? dismissedDate,
        DateOnly? reminderDate,
        DateTime? snoozeUntil)
    {
        var today = DateTimeHelper.GetLocalDate(now);
        return enabled &&
            DateTimeHelper.HasReachedTime(now, reminderTime) &&
            !hasLog &&
            dismissedDate != today &&
            (reminderDate != today || snoozeUntil is not null) &&
            (snoozeUntil is null || snoozeUntil <= now);
    }
}
