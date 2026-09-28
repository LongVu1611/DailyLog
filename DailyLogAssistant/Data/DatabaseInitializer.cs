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
        if (!await db.Settings.AnyAsync(cancellationToken))
        {
            db.Settings.Add(new AppSettings());
            await db.SaveChangesAsync(cancellationToken);
        }
        Log.Information("DatabaseInitialized");
    }
}
