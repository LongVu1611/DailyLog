using DailyLogAssistant.Data;
using DailyLogAssistant.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyLogAssistant.Services;

public sealed class SettingsService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Settings.SingleAsync(settings => settings.Id == 1, cancellationToken);
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.Settings.Update(settings);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SnoozeAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken);
        settings.SnoozeUntil = DateTime.Now.Add(duration);
        await SaveAsync(settings, cancellationToken);
    }

    public async Task DismissTodayAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken);
        settings.DismissedDate = DateOnly.FromDateTime(DateTime.Today);
        await SaveAsync(settings, cancellationToken);
    }
}
