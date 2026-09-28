using System.Text;
using System.IO;
using DailyLogAssistant.Models;
using Microsoft.Data.Sqlite;
using Serilog;

namespace DailyLogAssistant.Services;

public sealed class ExportService(LogService logs) : IExportService
{
    public async Task ExportCsvAsync(string path, CancellationToken cancellationToken = default)
    {
        Log.Information("ExportStarted: CSV to {Path}", path);
        var entries = await logs.SearchAsync(new LogQuery(), cancellationToken);
        await WriteCsvAsync(path, entries, cancellationToken);
    }

    public async Task ExportCsvAsync(string path, LogQuery query, CancellationToken cancellationToken = default)
    {
        var entries = await logs.SearchAsync(query, cancellationToken);
        await WriteCsvAsync(path, entries, cancellationToken);
    }

    private static async Task WriteCsvAsync(string path, List<LogEntry> entries, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder("Date,Category,Title,Project,Status,Recipient,Result,Problems,Notes,Tags,Created,Updated\r\n");
        foreach (var entry in entries)
            builder.AppendJoin(',', Csv(entry.Date.ToString("yyyy-MM-dd")), Csv(entry.Category?.Name ?? ""),
                Csv(entry.Title), Csv(entry.Project), Csv(entry.Status), Csv(entry.Recipient),
                Csv(entry.Result), Csv(entry.Problems), Csv(entry.Notes), Csv(entry.TagsSummary),
                Csv(entry.CreatedAt.ToString("O")), Csv(entry.UpdatedAt.ToString("O"))).Append("\r\n");
        await File.WriteAllTextAsync(path, builder.ToString(), new UTF8Encoding(true), cancellationToken);
        Log.Information("ExportCompleted: CSV to {Path}", path);
    }

    public async Task ExportMarkdownAsync(string path, CancellationToken cancellationToken = default)
    {
        Log.Information("ExportStarted: Markdown to {Path}", path);
        var entries = await logs.SearchAsync(new LogQuery(), cancellationToken);
        await WriteMarkdownAsync(path, entries, cancellationToken);
    }

    public async Task ExportMarkdownAsync(string path, LogQuery query, CancellationToken cancellationToken = default)
    {
        var entries = await logs.SearchAsync(query, cancellationToken);
        await WriteMarkdownAsync(path, entries, cancellationToken);
    }

    private static async Task WriteMarkdownAsync(string path, List<LogEntry> entries, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder("# Personal Log Archive\n");
        foreach (var entry in entries)
        {
            if (entry.Category?.Name == "LETTER")
            {
                builder.AppendLine().Append(LetterMarkdown(entry));
                continue;
            }
            builder.Append($"\n## {entry.Date:yyyy-MM-dd} · {entry.Category?.Name} · {entry.Title}\n\n");
            if (!string.IsNullOrWhiteSpace(entry.Project)) AppendSection(builder, "Project", entry.Project);
            if (!string.IsNullOrWhiteSpace(entry.Recipient)) AppendSection(builder, "Recipient", entry.Recipient);
            if (!string.IsNullOrWhiteSpace(entry.Body)) AppendSection(builder, "Letter", entry.Body);
            if (!string.IsNullOrWhiteSpace(entry.Content)) AppendSection(builder, "Content", entry.Content);
            if (!string.IsNullOrWhiteSpace(entry.Result)) AppendSection(builder, "Result", entry.Result);
            if (!string.IsNullOrWhiteSpace(entry.Problems)) AppendSection(builder, "Problems", entry.Problems);
            if (!string.IsNullOrWhiteSpace(entry.Notes)) AppendSection(builder, "Notes", entry.Notes);
            if (!string.IsNullOrWhiteSpace(entry.TagsSummary)) AppendSection(builder, "Tags", entry.TagsSummary);
        }
        await File.WriteAllTextAsync(path, builder.ToString(), new UTF8Encoding(false), cancellationToken);
        Log.Information("ExportCompleted: Markdown to {Path}", path);
    }

    public async Task BackupDatabaseAsync(string path, CancellationToken cancellationToken = default)
    {
        Log.Information("ExportStarted: database backup to {Path}", path);
        var sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PersonalLogManager", "PersonalLogManager.db");
        await using var source = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        await source.OpenAsync(cancellationToken);
        await destination.OpenAsync(cancellationToken);
        source.BackupDatabase(destination);
        Log.Information("ExportCompleted: database backup to {Path}", path);
    }

    public Task ExportDatabaseAsync(string path, CancellationToken cancellationToken = default) =>
        BackupDatabaseAsync(path, cancellationToken);

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    private static string LetterMarkdown(LogEntry entry)
    {
        var output = new StringBuilder("# ").AppendLine(entry.Title).AppendLine();
        if (!string.IsNullOrWhiteSpace(entry.Opening)) output.AppendLine(entry.Opening).AppendLine();
        else if (!string.IsNullOrWhiteSpace(entry.Recipient))
            output.Append("Gửi ").Append(entry.Recipient.Trim().TrimEnd(',', ':')).AppendLine(",").AppendLine();
        if (!string.IsNullOrWhiteSpace(entry.Body)) output.AppendLine(entry.Body).AppendLine();
        if (!string.IsNullOrWhiteSpace(entry.Closing)) output.AppendLine(entry.Closing).AppendLine();
        if (!string.IsNullOrWhiteSpace(entry.Signature)) output.AppendLine(entry.Signature);
        return output.ToString();
    }
    private static void AppendSection(StringBuilder output, string heading, string value)
    {
        output.Append("### ").Append(heading).Append("\n\n");
        output.Append(string.IsNullOrWhiteSpace(value) ? "_No entry._" : value).Append("\n\n");
    }
}
