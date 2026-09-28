namespace DailyLogAssistant.Models;

public sealed class AppSettings
{
    public int Id { get; set; } = 1;
    public string ReminderTime { get; set; } = "17:00";
    public bool ReminderEnabled { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool LaunchLogAutomatically { get; set; }
    public bool ShowNotification { get; set; } = true;
    public DateOnly? ReminderDate { get; set; }
    public DateOnly? DismissedDate { get; set; }
    public DateTime? SnoozeUntil { get; set; }
}
