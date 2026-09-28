using DailyLogAssistant.Data;
using DailyLogAssistant.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DailyLogAssistant.Services;

public sealed class LogService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<DailyLog>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var query = db.DailyLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(log => log.WorkDone.ToLower().Contains(term) ||
                log.Problems.ToLower().Contains(term) || log.TomorrowPlan.ToLower().Contains(term) ||
                log.Notes.ToLower().Contains(term));
        }
        return await query.OrderByDescending(log => log.Date).ToListAsync(cancellationToken);
    }

    public async Task<DailyLog?> GetAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.DailyLogs.AsNoTracking().SingleOrDefaultAsync(log => log.Date == date, cancellationToken);
    }

    public async Task SaveAsync(DailyLog draft, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(draft.WorkDone + draft.Problems + draft.TomorrowPlan + draft.Notes))
            throw new InvalidOperationException("Please write at least something about your day.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var existing = await db.DailyLogs.SingleOrDefaultAsync(log => log.Date == draft.Date, cancellationToken);
        var now = DateTime.Now;
        if (existing is null)
        {
            draft.CreatedAt = now;
            draft.UpdatedAt = now;
            db.DailyLogs.Add(draft);
            Log.Information("DailyLogCreated for {Date}", draft.Date);
        }
        else
        {
            existing.WorkDone = draft.WorkDone;
            existing.Problems = draft.Problems;
            existing.TomorrowPlan = draft.TomorrowPlan;
            existing.Notes = draft.Notes;
            existing.UpdatedAt = now;
            Log.Information("DailyLogUpdated for {Date}", draft.Date);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.DailyLogs.Where(log => log.Date == date).ExecuteDeleteAsync(cancellationToken);
    }
}
