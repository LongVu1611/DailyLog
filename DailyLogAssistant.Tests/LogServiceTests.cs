using DailyLogAssistant.Data;
using DailyLogAssistant.Models;
using DailyLogAssistant.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public LogService Logs { get; }

        private TestDatabase(SqliteConnection connection, LogService logs)
        {
            _connection = connection;
            Logs = logs;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var factory = new TestContextFactory(connection);
            await using (var context = await factory.CreateDbContextAsync())
                await context.Database.EnsureCreatedAsync();
            return new TestDatabase(connection, new LogService(factory));
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
