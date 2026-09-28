using Microsoft.Data.Sqlite;
using System.IO;

namespace DailyLogAssistant.Data;

public static class LegacyDatabaseMigrator
{
    public static async Task CopyDailyLogDatabaseAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(destinationPath)) return;
        var legacyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DailyLogAssistant", "DailyLogAssistant.db");
        if (!File.Exists(legacyPath)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await using var source = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = legacyPath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = destinationPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        await source.OpenAsync(cancellationToken);
        await destination.OpenAsync(cancellationToken);
        source.BackupDatabase(destination);
    }
}
