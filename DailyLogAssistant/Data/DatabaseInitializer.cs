using DailyLogAssistant.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DailyLogAssistant.Data;

public sealed class DatabaseInitializer(IDbContextFactory<AppDbContext> factory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AppSettings" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AppSettings" PRIMARY KEY,
                "ReminderTime" TEXT NOT NULL DEFAULT '17:00',
                "ReminderEnabled" INTEGER NOT NULL DEFAULT 1,
                "StartWithWindows" INTEGER NOT NULL DEFAULT 0,
                "MinimizeToTray" INTEGER NOT NULL DEFAULT 1,
                "LaunchLogAutomatically" INTEGER NOT NULL DEFAULT 0,
                "ShowNotification" INTEGER NOT NULL DEFAULT 1,
                "ReminderDate" TEXT NULL,
                "DismissedDate" TEXT NULL,
                "SnoozeUntil" TEXT NULL,
                "Theme" TEXT NOT NULL DEFAULT 'System',
                "AccentColor" TEXT NOT NULL DEFAULT '#315C4C',
                "Language" TEXT NOT NULL DEFAULT 'English',
                "WorkReportTemplatePath" TEXT NOT NULL DEFAULT ''
            );
            CREATE TABLE IF NOT EXISTS "Categories" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Categories" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL, "Icon" TEXT NOT NULL, "Color" TEXT NOT NULL, "IsBuiltIn" INTEGER NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Categories_Name" ON "Categories" ("Name");
            CREATE TABLE IF NOT EXISTS "Tags" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Tags" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL, "Color" TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Tags_Name" ON "Tags" ("Name");
            CREATE TABLE IF NOT EXISTS "Logs" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Logs" PRIMARY KEY AUTOINCREMENT,
                "CategoryId" INTEGER NOT NULL, "Date" TEXT NOT NULL, "Title" TEXT NOT NULL,
                "Project" TEXT NOT NULL, "Status" TEXT NOT NULL, "Recipient" TEXT NOT NULL,
                "Mood" TEXT NOT NULL, "Opening" TEXT NOT NULL, "Body" TEXT NOT NULL,
                "Closing" TEXT NOT NULL, "Signature" TEXT NOT NULL, "Content" TEXT NOT NULL,
                "ThingsToRemember" TEXT NOT NULL, "Result" TEXT NOT NULL, "Problems" TEXT NOT NULL,
                "Notes" TEXT NOT NULL, "TagsText" TEXT NOT NULL, "CreatedAt" TEXT NOT NULL, "UpdatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_Logs_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS "IX_Logs_Date" ON "Logs" ("Date");
            CREATE INDEX IF NOT EXISTS "IX_Logs_CategoryId" ON "Logs" ("CategoryId");
            CREATE INDEX IF NOT EXISTS "IX_Logs_Status" ON "Logs" ("Status");
            CREATE INDEX IF NOT EXISTS "IX_Logs_Title" ON "Logs" ("Title");
            CREATE TABLE IF NOT EXISTS "LogTags" (
                "LogEntryId" INTEGER NOT NULL, "TagId" INTEGER NOT NULL,
                CONSTRAINT "PK_LogTags" PRIMARY KEY ("LogEntryId", "TagId"),
                CONSTRAINT "FK_LogTags_Logs_LogEntryId" FOREIGN KEY ("LogEntryId") REFERENCES "Logs" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_LogTags_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES "Tags" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_LogTags_TagId" ON "LogTags" ("TagId");
            """, cancellationToken);

        if (!await SettingsColumnExistsAsync(db, "Language", cancellationToken))
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "AppSettings" ADD COLUMN "Language" TEXT NOT NULL DEFAULT 'English'""",
                cancellationToken);

        foreach (var category in BuiltInCategories.All)
        {
            if (!await db.Categories.AnyAsync(item => item.Name == category.Name, cancellationToken))
                db.Categories.Add(category);
        }
        await db.SaveChangesAsync(cancellationToken);

        if (await TableExistsAsync(db, "Settings", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync("""
                INSERT OR IGNORE INTO AppSettings
                    (Id, ReminderTime, ReminderEnabled, StartWithWindows, MinimizeToTray,
                     LaunchLogAutomatically, ShowNotification, ReminderDate, DismissedDate, SnoozeUntil)
                SELECT Id, ReminderTime, ReminderEnabled, StartWithWindows, MinimizeToTray,
                       LaunchLogAutomatically, ShowNotification, ReminderDate, DismissedDate, SnoozeUntil
                FROM Settings
                """, cancellationToken);
        }
        if (!await db.Settings.AnyAsync(cancellationToken))
        {
            db.Settings.Add(new AppSettings());
            await db.SaveChangesAsync(cancellationToken);
        }

        if (await TableExistsAsync(db, "DailyLogs", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO Logs
                    (CategoryId, Date, Title, Project, Status, Recipient, Mood, Opening, Body,
                     Closing, Signature, Content, ThingsToRemember, Result, Problems, Notes,
                     TagsText, CreatedAt, UpdatedAt)
                SELECT 1, Date, 'Daily work log', '', 'Completed', '', '', '', '',
                       '', '', WorkDone, '', WorkDone, Problems,
                       CASE WHEN length(TomorrowPlan) > 0 THEN 'Tomorrow: ' || TomorrowPlan || char(10) ELSE '' END || Notes,
                       '', CreatedAt, UpdatedAt
                FROM DailyLogs
                WHERE NOT EXISTS (SELECT 1 FROM Logs WHERE Logs.CategoryId = 1 AND Logs.Date = DailyLogs.Date)
                """, cancellationToken);
        }
        Log.Information("DatabaseInitialized");
    }

    private static async Task<bool> TableExistsAsync(
        AppDbContext db, string name, CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = name;
        command.Parameters.Add(parameter);
        if (command.Connection?.State != System.Data.ConnectionState.Open)
            await db.Database.OpenConnectionAsync(cancellationToken);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    private static async Task<bool> SettingsColumnExistsAsync(
        AppDbContext db, string columnName, CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """PRAGMA table_info("AppSettings")""";
        if (command.Connection?.State != System.Data.ConnectionState.Open)
            await db.Database.OpenConnectionAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}

public static class BuiltInCategories
{
    public static LogCategory[] All =>
    [
        new() { Id = 1, Name = "WORK", Icon = "▣", Color = "#315C4C", IsBuiltIn = true },
        new() { Id = 2, Name = "PERSONAL", Icon = "♡", Color = "#8C5B73", IsBuiltIn = true },
        new() { Id = 3, Name = "LETTER", Icon = "✉", Color = "#536A9B", IsBuiltIn = true },
        new() { Id = 4, Name = "NOTE", Icon = "✎", Color = "#A06E2E", IsBuiltIn = true }
    ];
}
