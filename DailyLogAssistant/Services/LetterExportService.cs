using System.Text;
using System.IO;
using DailyLogAssistant.Models;
using Serilog;

namespace DailyLogAssistant.Services;

public sealed class LetterExportService : ILetterExportService
{
    public async Task ExportTxtAsync(LogEntry entry, string path, CancellationToken cancellationToken = default)
    {
        var text = BuildText(entry);
        await File.WriteAllTextAsync(path, text, new UTF8Encoding(false), cancellationToken);
        Log.Information("Letter exported as plain text to {Path}", path);
    }

    public async Task ExportMarkdownAsync(LogEntry entry, string path, CancellationToken cancellationToken = default)
    {
        var output = new StringBuilder("# ").AppendLine(entry.Title).AppendLine();
        AppendSalutation(output, entry);
        if (!string.IsNullOrWhiteSpace(entry.Body)) output.AppendLine(entry.Body).AppendLine();
        if (!string.IsNullOrWhiteSpace(entry.Closing)) output.AppendLine(entry.Closing).AppendLine();
        if (!string.IsNullOrWhiteSpace(entry.Signature)) output.AppendLine(entry.Signature);
        await File.WriteAllTextAsync(path, output.ToString(), new UTF8Encoding(false), cancellationToken);
        Log.Information("Letter exported as Markdown to {Path}", path);
    }

    public static string BuildText(LogEntry entry)
    {
        var output = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(entry.Title))
            output.AppendLine("## ").AppendLine(entry.Title).AppendLine();
        AppendSalutation(output, entry);
        if (!string.IsNullOrWhiteSpace(entry.Body)) output.AppendLine(entry.Body).AppendLine();
        if (!string.IsNullOrWhiteSpace(entry.Closing)) output.AppendLine(entry.Closing).AppendLine();
        if (!string.IsNullOrWhiteSpace(entry.Signature)) output.AppendLine(entry.Signature);
        return output.ToString();
    }

    private static void AppendSalutation(StringBuilder output, LogEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Opening))
            output.AppendLine(entry.Opening).AppendLine();
        else if (!string.IsNullOrWhiteSpace(entry.Recipient))
            output.Append("Gửi ").Append(entry.Recipient.Trim().TrimEnd(',', ':')).AppendLine(",").AppendLine();
    }
}
