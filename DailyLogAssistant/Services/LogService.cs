using DailyLogAssistant.Data;
using DailyLogAssistant.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DailyLogAssistant.Services;

public sealed class LogService(IDbContextFactory<AppDbContext> factory) : ILogService
{
    public async Task<List<LogEntry>> SearchAsync(LogQuery filter, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var query = db.Logs.AsNoTracking().Include(log => log.Category)
            .Include(log => log.LogTags).ThenInclude(link => link.Tag).AsQueryable();
        if (filter.CategoryId is { } categoryId) query = query.Where(log => log.CategoryId == categoryId);
        if (filter.TagId is { } tagId) query = query.Where(log => log.LogTags.Any(link => link.TagId == tagId));
        if (filter.From is { } from) query = query.Where(log => log.Date >= from);
        if (filter.To is { } to) query = query.Where(log => log.Date <= to);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(log => log.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(log => log.Title.ToLower().Contains(term) ||
                log.Content.ToLower().Contains(term) || log.Project.ToLower().Contains(term) ||
                log.Result.ToLower().Contains(term) || log.Problems.ToLower().Contains(term) ||
                log.Notes.ToLower().Contains(term) || log.Recipient.ToLower().Contains(term) ||
                log.Body.ToLower().Contains(term) ||
                log.LogTags.Any(link => link.Tag.Name.ToLower().Contains(term)));
        }
        return await query.OrderByDescending(log => log.Date).ThenByDescending(log => log.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<LogEntry?> GetLogAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Logs.Include(log => log.Category).Include(log => log.LogTags)
            .ThenInclude(link => link.Tag).SingleOrDefaultAsync(log => log.Id == id, cancellationToken);
    }

    public async Task SaveLogAsync(LogEntry draft, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(draft.Title) &&
            string.IsNullOrWhiteSpace(draft.Content + draft.Body + draft.Project + draft.Result +
                draft.Problems + draft.Notes + draft.Recipient + draft.ThingsToRemember))
            throw new InvalidOperationException("Please add a title or some content before saving.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (!await db.Categories.AnyAsync(category => category.Id == draft.CategoryId, cancellationToken))
            throw new InvalidOperationException("Select a valid category.");
        var tagNames = draft.TagsText.Split([',', ';', '#'], StringSplitOptions.TrimEntries |
                StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var tags = new List<Tag>();
        foreach (var name in tagNames)
        {
            var tag = await db.Tags.FirstOrDefaultAsync(item => item.Name.ToLower() == name.ToLower(), cancellationToken);
            if (tag is null)
            {
                tag = new Tag { Name = name, Color = "#315C4C" };
                db.Tags.Add(tag);
                await db.SaveChangesAsync(cancellationToken);
            }
            tags.Add(tag);
        }

        LogEntry entry;
        if (draft.Id == 0)
        {
            entry = draft;
            entry.CreatedAt = DateTime.Now;
            db.Logs.Add(entry);
        }
        else
        {
            entry = await db.Logs.Include(log => log.LogTags)
                .SingleOrDefaultAsync(log => log.Id == draft.Id, cancellationToken)
                ?? throw new KeyNotFoundException("The selected log no longer exists.");
            entry.CategoryId = draft.CategoryId;
            entry.Date = draft.Date;
            entry.Title = draft.Title.Trim();
            entry.Project = draft.Project;
            entry.Status = draft.Status;
            entry.Recipient = draft.Recipient;
            entry.Mood = draft.Mood;
            entry.Opening = draft.Opening;
            entry.Body = draft.Body;
            entry.Closing = draft.Closing;
            entry.Signature = draft.Signature;
            entry.Content = draft.Content;
            entry.ThingsToRemember = draft.ThingsToRemember;
            entry.Result = draft.Result;
            entry.Problems = draft.Problems;
            entry.Notes = draft.Notes;
            entry.TagsText = string.Join(", ", tagNames);
            db.LogTags.RemoveRange(entry.LogTags);
        }
        entry.Title = draft.Title.Trim();
        entry.TagsText = string.Join(", ", tagNames);
        entry.UpdatedAt = DateTime.Now;
        foreach (var tag in tags) entry.LogTags.Add(new LogTag { Log = entry, Tag = tag });
        await db.SaveChangesAsync(cancellationToken);
        Log.Information("LogSaved {LogId} in {CategoryId}", entry.Id, entry.CategoryId);
    }

    public async Task DeleteLogAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Logs.Where(log => log.Id == id).ExecuteDeleteAsync(cancellationToken);
        Log.Information("LogDeleted {LogId}", id);
    }

    public Task<List<LogEntry>> GetWorkLogsAsync(
        DateOnly from, DateOnly? to = null, CancellationToken cancellationToken = default) =>
        SearchAsync(new LogQuery(CategoryId: 1, From: from, To: to), cancellationToken);

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
