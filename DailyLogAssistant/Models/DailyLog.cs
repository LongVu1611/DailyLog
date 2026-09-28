namespace DailyLogAssistant.Models;

public sealed class DailyLog
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string WorkDone { get; set; } = "";
    public string Problems { get; set; } = "";
    public string TomorrowPlan { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
