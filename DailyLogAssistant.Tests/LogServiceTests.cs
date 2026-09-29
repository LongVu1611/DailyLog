using DailyLogAssistant.Data;
using DailyLogAssistant.Localization;
using DailyLogAssistant.Models;
using DailyLogAssistant.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using Xunit;

namespace DailyLogAssistant.Tests;

public sealed class LogServiceTests
{
    [Fact]
    public async Task SaveAsync_UpsertsByDate_AndSearchesAllFields()
    {
        await using var database = await TestDatabase.CreateAsync();
        var date = DateOnly.FromDateTime(DateTime.Today);
        await database.Logs.SaveAsync(new DailyLog
        {
            Date = date,
            WorkDone = "Prepared a release",
            Problems = "Blocked on integration"
        });
        await database.Logs.SaveAsync(new DailyLog
        {
            Date = date,
            WorkDone = "Finished release",
            Notes = "Follow up with team"
        });

        var result = await database.Logs.GetAllAsync("follow UP");
        Assert.Single(result);
        Assert.Equal("Finished release", (await database.Logs.GetAsync(date))!.WorkDone);
    }

    [Fact]
    public async Task SaveAsync_RejectsAnEntryWithNoContent()
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            database.Logs.SaveAsync(new DailyLog { Date = DateOnly.FromDateTime(DateTime.Today) }));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheSelectedDay()
    {
        await using var database = await TestDatabase.CreateAsync();
        var date = DateOnly.FromDateTime(DateTime.Today);
        await database.Logs.SaveAsync(new DailyLog { Date = date, Notes = "Keep this" });

        await database.Logs.DeleteAsync(date);

        Assert.Null(await database.Logs.GetAsync(date));
    }

    [Fact]
    public async Task Statistics_CalculatesCurrentAndLongestStreak()
    {
        await using var database = await TestDatabase.CreateAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var date in new[] { today.AddDays(-1), today, today.AddDays(-5), today.AddDays(-4) })
            await database.Logs.SaveAsync(new DailyLog { Date = date, WorkDone = "Done" });

        var result = await new StatisticsService(database.Logs).GetAsync();

        Assert.Equal(4, result.Total);
        Assert.Equal(2, result.CurrentStreak);
        Assert.Equal(2, result.LongestStreak);
        Assert.Equal(new[] { today, today.AddDays(-1), today.AddDays(-5), today.AddDays(-4) }
            .Count(date => date.Year == today.Year && date.Month == today.Month), result.ThisMonth);
    }

    [Fact]
    public async Task GenericLogs_SaveTagsAndSearchAcrossCategorySpecificFields()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = await database.Factory.CreateDbContextAsync();
        var category = context.Categories.Single(item => item.Name == "WORK");
        var work = new LogEntry
        {
            CategoryId = category.Id, Date = new DateOnly(2026, 9, 21), Title = "Weekly task",
            Project = "FusionCompute PoC", Status = "Completed", Result = "Validated", TagsText = "Huawei, PoC"
        };
        await database.Logs.SaveLogAsync(work);

        var matches = await database.Logs.SearchAsync(new LogQuery(Search: "fusioncompute"));
        var byTag = await database.Logs.SearchAsync(new LogQuery(TagId: matches.Single().LogTags[0].TagId));

        Assert.Single(matches);
        Assert.Equal("Weekly task", matches[0].Title);
        Assert.Single(byTag);
        Assert.Equal(2, matches[0].LogTags.Count);
    }

    [Fact]
    public async Task Search_FiltersByCategoryAndDateRange()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = await database.Factory.CreateDbContextAsync();
        var work = context.Categories.Single(item => item.Name == "WORK");
        var personal = context.Categories.Single(item => item.Name == "PERSONAL");
        context.Logs.AddRange(
            new LogEntry { CategoryId = work.Id, Date = new DateOnly(2026, 9, 21), Title = "Work" },
            new LogEntry { CategoryId = personal.Id, Date = new DateOnly(2026, 9, 22), Title = "Personal" });
        await context.SaveChangesAsync();

        var results = await database.Logs.SearchAsync(new LogQuery(CategoryId: work.Id,
            From: new DateOnly(2026, 9, 21), To: new DateOnly(2026, 9, 21)));

        Assert.Single(results);
        Assert.Equal("Work", results[0].Title);
    }

    [Fact]
    public async Task GetRecentAsync_LimitsResultsAndIncludesCategories()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = await database.Factory.CreateDbContextAsync();
        var work = context.Categories.Single(category => category.Name == "WORK");
        context.Logs.AddRange(
            new LogEntry { CategoryId = work.Id, Date = new DateOnly(2026, 9, 20), Title = "Older" },
            new LogEntry { CategoryId = work.Id, Date = new DateOnly(2026, 9, 21), Title = "Recent" },
            new LogEntry { CategoryId = work.Id, Date = new DateOnly(2026, 9, 22), Title = "Newest" });
        await context.SaveChangesAsync();

        var results = await database.Logs.GetRecentAsync(2);

        Assert.Equal(new[] { "Newest", "Recent" }, results.Select(log => log.Title));
        Assert.All(results, log => Assert.Equal("WORK", log.Category!.Name));
    }

    [Fact]
    public async Task Tags_CanBeRenamedRecoloredAndDeleted()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = await database.Factory.CreateDbContextAsync();
        var work = context.Categories.Single(category => category.Name == "WORK");
        await database.Logs.SaveLogAsync(new LogEntry
        {
            CategoryId = work.Id, Date = new DateOnly(2026, 9, 21), Title = "Tagged log", TagsText = "old-tag"
        });
        var tagService = new TagService(database.Factory);
        var tag = (await tagService.GetTagsAsync()).Single();

        await tagService.SaveTagAsync("new-tag", "#2878B5", tag.Id);

        Assert.Empty(await database.Logs.SearchAsync(new LogQuery(Search: "old-tag")));
        Assert.Single(await database.Logs.SearchAsync(new LogQuery(Search: "new-tag")));
        Assert.Equal("#2878B5", (await tagService.GetTagsAsync()).Single().Color);

        await tagService.DeleteTagAsync(tag.Id);
        Assert.Empty(await tagService.GetTagsAsync());
        Assert.Empty(await database.Logs.SearchAsync(new LogQuery(TagId: tag.Id)));
    }

    [Fact]
    public async Task Categories_AllowCustomIconAndColorButProtectBuiltIns()
    {
        await using var database = await TestDatabase.CreateAsync();
        var categories = new CategoryService(database.Factory);
        var custom = await categories.CreateCustomCategoryAsync("Learning", "✦", "#7953A9");

        Assert.Equal("✦  Learning", custom.DisplayName);
        await Assert.ThrowsAsync<InvalidOperationException>(() => categories.DeleteCustomCategoryAsync(1));
        await categories.DeleteCustomCategoryAsync(custom.Id);
        Assert.DoesNotContain(await categories.GetCategoriesAsync(), category => category.Name == "Learning");
    }

    [Fact]
    public async Task SettingsService_PersistsThemeAndAccentColor()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new SettingsService(database.Factory);
        var settings = await service.GetAsync();
        settings.Theme = "Dark";
        settings.AccentColor = "#7953A9";
        settings.Language = "Русский";
        await service.SaveAsync(settings);

        var actual = await service.GetAsync();
        Assert.Equal("Dark", actual.Theme);
        Assert.Equal("#7953A9", actual.AccentColor);
        Assert.Equal("Русский", actual.Language);
    }

    [Fact]
    public void Localization_TranslatesInterfaceAndUsesLanguageCulture()
    {
        try
        {
            LocalizationService.SetLanguage("Tiếng Việt");
            Assert.Equal("Tổng quan", LocalizationService.Translate("Dashboard"));
            Assert.Equal("vi-VN", LocalizationService.Instance.CurrentCulture.Name);
            Assert.Equal("Cần nhập tên danh mục.",
                LocalizationService.TranslateException(new ArgumentException("Category name is required. (Parameter 'name')")));

            LocalizationService.SetLanguage("Русский");
            Assert.Equal("Главная", LocalizationService.Translate("Dashboard"));
            Assert.Equal("ru-RU", LocalizationService.Instance.CurrentCulture.Name);

            LocalizationService.SetLanguage("unsupported");
            Assert.Equal("Dashboard", LocalizationService.Translate("Dashboard"));
            Assert.Equal("en-US", LocalizationService.Instance.CurrentCulture.Name);
        }
        finally
        {
            LocalizationService.SetLanguage("English");
        }
    }

    [Fact]
    public async Task DatabaseInitializer_AddsLanguageToAnExistingSettingsTable()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                CREATE TABLE AppSettings (
                    Id INTEGER NOT NULL PRIMARY KEY,
                    ReminderTime TEXT NOT NULL DEFAULT '17:00',
                    ReminderEnabled INTEGER NOT NULL DEFAULT 1,
                    StartWithWindows INTEGER NOT NULL DEFAULT 0,
                    MinimizeToTray INTEGER NOT NULL DEFAULT 1,
                    LaunchLogAutomatically INTEGER NOT NULL DEFAULT 0,
                    ShowNotification INTEGER NOT NULL DEFAULT 1,
                    ReminderDate TEXT NULL,
                    DismissedDate TEXT NULL,
                    SnoozeUntil TEXT NULL,
                    Theme TEXT NOT NULL DEFAULT 'System',
                    AccentColor TEXT NOT NULL DEFAULT '#315C4C',
                    WorkReportTemplatePath TEXT NOT NULL DEFAULT ''
                );
                INSERT INTO AppSettings (Id, ReminderTime) VALUES (1, '18:30');
                """;
            await command.ExecuteNonQueryAsync();
        }
        var factory = new TestContextFactory(connection);

        await new DatabaseInitializer(factory).InitializeAsync();

        await using var db = await factory.CreateDbContextAsync();
        var settings = await db.Settings.SingleAsync();
        Assert.Equal("English", settings.Language);
        Assert.Equal("18:30", settings.ReminderTime);
    }

    [Fact]
    public async Task LetterExport_WritesPlainTextWithoutDatabaseMetadata()
    {
        var path = Path.Combine(Path.GetTempPath(), $"letter-{Guid.NewGuid():N}.txt");
        try
        {
            var entry = new LogEntry
            {
                Id = 12345, Date = new DateOnly(2026, 9, 28), Title = "A note",
                Opening = "Gửi em,", Body = "Chào em.", Closing = "Thương em,", Signature = "Vũ"
            };
            var exporter = new LetterExportService();
            await exporter.ExportTxtAsync(entry, path);

            var text = await File.ReadAllTextAsync(path);
            Assert.Contains("Gửi em,", text);
            Assert.Contains("Chào em.", text);
            Assert.DoesNotContain("12345", text);
            Assert.DoesNotContain("2026-09-28", text);

            await exporter.ExportMarkdownAsync(entry, path);
            var markdown = await File.ReadAllTextAsync(path);
            Assert.StartsWith("# A note", markdown);
            Assert.Contains("Thương em,", markdown);
            Assert.DoesNotContain("12345", markdown);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task WeeklyExcelReport_SeparatesCompletedIncompleteAndNextWeek()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using (var context = await database.Factory.CreateDbContextAsync())
        {
            var work = context.Categories.Single(category => category.Name == "WORK");
            context.Logs.AddRange(
                new LogEntry { CategoryId = work.Id, Date = new DateOnly(2026, 9, 21), Title = "Done", Status = "Completed", Result = "Shipped" },
                new LogEntry { CategoryId = work.Id, Date = new DateOnly(2026, 9, 22), Title = "Blocked", Status = "Blocked", Problems = "Waiting" },
                new LogEntry { CategoryId = work.Id, Date = new DateOnly(2026, 9, 28), Title = "Next", Status = "Planned" });
            await context.SaveChangesAsync();
        }
        var path = Path.Combine(Path.GetTempPath(), $"work-report-{Guid.NewGuid():N}.xlsx");
        try
        {
            await new ExcelReportService(database.Factory, database.Logs).ExportWeeklyReportAsync(
                new DateOnly(2026, 9, 21), new DateOnly(2026, 9, 25), path);

            using var workbook = new XLWorkbook(path);
            var text = string.Join(" ", workbook.Worksheet(1).CellsUsed().Select(cell => cell.GetString()));
            Assert.Contains("VIỆC ĐÃ HOÀN THÀNH TRONG TUẦN", text);
            Assert.Contains("VIỆC CHƯA HOÀN THÀNH TRONG TUẦN", text);
            Assert.Contains("KẾ HOẠCH CÔNG VIỆC TUẦN TIẾP THEO", text);
            Assert.Contains("Done", text);
            Assert.Contains("Blocked", text);
            Assert.Contains("Next", text);
            var nextRow = workbook.Worksheet(1).CellsUsed()
                .Single(cell => cell.GetString() == "Next").Address.RowNumber;
            Assert.Equal("", workbook.Worksheet(1).Cell(nextRow, 4).GetString());
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task WeeklyExcelReport_UsesTemplateMappingAndExpandsRowsWithoutReplacingSections()
    {
        await using var database = await TestDatabase.CreateAsync();
        var sourceTemplate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "DailyLogAssistant", "Resources", "WorkReportTemplate.xlsx"));
        Assert.True(File.Exists(sourceTemplate));
        await using (var db = await database.Factory.CreateDbContextAsync())
        {
            var settings = await db.Settings.SingleAsync();
            settings.WorkReportTemplatePath = sourceTemplate;
            var work = db.Categories.Single(category => category.Name == "WORK");
            for (var index = 0; index < 5; index++)
                db.Logs.Add(new LogEntry
                {
                    CategoryId = work.Id, Date = new DateOnly(2026, 9, 21).AddDays(index),
                    Title = $"Template task {index + 1}", Status = "Completed", Result = "Done"
                });
            await db.SaveChangesAsync();
        }
        var path = Path.Combine(Path.GetTempPath(), $"template-report-{Guid.NewGuid():N}.xlsx");
        try
        {
            await new ExcelReportService(database.Factory, database.Logs).ExportWeeklyReportAsync(
                new DateOnly(2026, 9, 21), new DateOnly(2026, 9, 25), path);
            using var workbook = new XLWorkbook(path);
            var cells = workbook.Worksheet(1).CellsUsed().Select(cell => cell.GetString()).ToList();
            Assert.Contains(cells, value => value == "Template task 5");
            Assert.Contains(cells, value => value.Contains("VIỆC CHƯA HOÀN THÀNH", StringComparison.Ordinal));
            Assert.Contains(cells, value => value.Contains("KẾ HOẠCH CÔNG VIỆC", StringComparison.Ordinal));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void DefaultExcelTemplate_ContainsMappedSectionsAndHeadings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"work-template-{Guid.NewGuid():N}.xlsx");
        try
        {
            ExcelReportService.CreateDefaultTemplate(path);
            using var workbook = new XLWorkbook(path);
            var values = workbook.Worksheet(1).CellsUsed().Select(cell => cell.GetString()).ToList();
            Assert.Contains(values, value => value.Contains("VIỆC ĐÃ HOÀN THÀNH", StringComparison.Ordinal));
            Assert.Contains(values, value => value.Contains("VIỆC CHƯA HOÀN THÀNH", StringComparison.Ordinal));
            Assert.Contains(values, value => value.Contains("KẾ HOẠCH CÔNG VIỆC", StringComparison.Ordinal));
            Assert.Contains(values, value => value == "STT");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task DatabaseInitializer_ImportsExistingDailyLogsAndReminderSettings()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                CREATE TABLE DailyLogs (
                    Id INTEGER PRIMARY KEY, Date TEXT NOT NULL, WorkDone TEXT NOT NULL,
                    Problems TEXT NOT NULL, TomorrowPlan TEXT NOT NULL, Notes TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL);
                CREATE TABLE Settings (
                    Id INTEGER PRIMARY KEY, ReminderTime TEXT NOT NULL, ReminderEnabled INTEGER NOT NULL,
                    StartWithWindows INTEGER NOT NULL, MinimizeToTray INTEGER NOT NULL,
                    LaunchLogAutomatically INTEGER NOT NULL, ShowNotification INTEGER NOT NULL,
                    ReminderDate TEXT NULL, DismissedDate TEXT NULL, SnoozeUntil TEXT NULL);
                INSERT INTO DailyLogs VALUES (1, '2026-09-28', 'Migrated work', 'None',
                    'Review tomorrow', 'Important', '2026-09-28T08:00:00', '2026-09-28T08:00:00');
                INSERT INTO Settings VALUES (1, '18:15', 1, 0, 1, 0, 1, NULL, NULL, NULL);
                """;
            await command.ExecuteNonQueryAsync();
        }
        var factory = new TestContextFactory(connection);

        await new DatabaseInitializer(factory).InitializeAsync();

        var logs = await new LogService(factory).SearchAsync(new LogQuery(Search: "migrated work"));
        await using var db = await factory.CreateDbContextAsync();
        var settings = await db.Settings.SingleAsync();
        Assert.Single(logs);
        Assert.Equal("WORK", logs[0].Category!.Name);
        Assert.Equal("18:15", settings.ReminderTime);
        Assert.Contains("Review tomorrow", logs[0].Notes);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public LogService Logs { get; }
        public TestContextFactory Factory { get; }

        private TestDatabase(SqliteConnection connection, TestContextFactory factory, LogService logs)
        {
            _connection = connection;
            Factory = factory;
            Logs = logs;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var factory = new TestContextFactory(connection);
            await new DatabaseInitializer(factory).InitializeAsync();
            return new TestDatabase(connection, factory, new LogService(factory));
        }

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }

    private sealed class TestContextFactory(SqliteConnection connection) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            return new AppDbContext(options);
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
