using System.Text;
using System.IO;
using DailyLogAssistant.Models;
using Microsoft.Data.Sqlite;
using Serilog;

namespace DailyLogAssistant.Services;

public sealed class ExportService(LogService logs)
{
    public async Task ExportCsvAsync(string path, CancellationToken cancellationToken = default)
    {
        Log.Information("ExportStarted: CSV to {Path}", path);
        var entries = await logs.GetAllAsync(cancellationToken: cancellationToken);
        var builder = new StringBuilder("Date,Work Done,Problems,Tomorrow Plan,Notes,Created,Updated\r\n");
        foreach (var entry in entries)
            builder.AppendJoin(',', Csv(entry.Date.ToString("yyyy-MM-dd")), Csv(entry.WorkDone),
                Csv(entry.Problems), Csv(entry.TomorrowPlan), Csv(entry.Notes),
                Csv(entry.CreatedAt.ToString("O")), Csv(entry.UpdatedAt.ToString("O"))).Append("\r\n");
        await File.WriteAllTextAsync(path, builder.ToString(), new UTF8Encoding(true), cancellationToken);
        Log.Information("ExportCompleted: CSV to {Path}", path);
    }

    public async Task ExportMarkdownAsync(string path, CancellationToken cancellationToken = default)
    {
        Log.Information("ExportStarted: Markdown to {Path}", path);
        var entries = await logs.GetAllAsync(cancellationToken: cancellationToken);
        var builder = new StringBuilder("# Daily Log Archive\n");
        foreach (var entry in entries)
        {
            builder.Append($"\n## Daily Log - {entry.Date:yyyy-MM-dd}\n\n");
            AppendSection(builder, "Work Done", entry.WorkDone);
            AppendSection(builder, "Problems", entry.Problems);
            AppendSection(builder, "Tomorrow", entry.TomorrowPlan);
            AppendSection(builder, "Notes", entry.Notes);
        }
        await File.WriteAllTextAsync(path, builder.ToString(), new UTF8Encoding(false), cancellationToken);
        Log.Information("ExportCompleted: Markdown to {Path}", path);
    }

    public async Task BackupDatabaseAsync(string path, CancellationToken cancellationToken = default)
    {
        Log.Information("ExportStarted: database backup to {Path}", path);
        var sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DailyLogAssistant", "DailyLogAssistant.db");
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

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    private static void AppendSection(StringBuilder output, string heading, string value)
    {
        output.Append("### ").Append(heading).Append("\n\n");
        output.Append(string.IsNullOrWhiteSpace(value) ? "_No entry._" : value).Append("\n\n");
    }
}
