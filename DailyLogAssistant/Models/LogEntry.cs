using DailyLogAssistant.Localization;

namespace DailyLogAssistant.Models;

public sealed class LogEntry
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public LogCategory? Category { get; set; }
    public DateOnly Date { get; set; }
    public string Title { get; set; } = "";
    public string Project { get; set; } = "";
    public string Status { get; set; } = "Planned";
    public string Recipient { get; set; } = "";
    public string Mood { get; set; } = "";
    public string Opening { get; set; } = "";
    public string Body { get; set; } = "";
    public string Closing { get; set; } = "";
    public string Signature { get; set; } = "";
    public string Content { get; set; } = "";
    public string ThingsToRemember { get; set; } = "";
    public string Result { get; set; } = "";
    public string Problems { get; set; } = "";
    public string Notes { get; set; } = "";
    public string TagsText { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<LogTag> LogTags { get; set; } = [];
    public IReadOnlyList<string> Tags => LogTags.Select(link => link.Tag?.Name)
        .Where(name => name is not null).Cast<string>().ToArray();
    public string TagsSummary => string.Join(", ", Tags);
    public string StatusDisplay => LocalizationService.Translate(Status);
    public string DisplayContent => string.Join(Environment.NewLine,
        new[] { Project, Result, Problems, Body, Content, Notes }.Where(value => !string.IsNullOrWhiteSpace(value)));
}

public sealed class LogCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "●";
    public string Color { get; set; } = "#315C4C";
    public bool IsBuiltIn { get; set; }
    public string DisplayName => $"{Icon}  {LocalizationService.Translate(Name)}";
    public List<LogEntry> Logs { get; set; } = [];
}

public sealed class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#315C4C";
    public List<LogTag> LogTags { get; set; } = [];
}

public sealed class LogTag
{
    public int LogEntryId { get; set; }
    public LogEntry Log { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
