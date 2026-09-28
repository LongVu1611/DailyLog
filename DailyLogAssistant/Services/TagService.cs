using DailyLogAssistant.Data;
using DailyLogAssistant.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyLogAssistant.Services;

public sealed class TagService(IDbContextFactory<AppDbContext> factory) : ITagService
{
    public async Task<List<Tag>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Tags.AsNoTracking().OrderBy(tag => tag.Name).ToListAsync(cancellationToken);
    }

    public async Task<Tag> SaveTagAsync(
        string name, string color, int? id = null, CancellationToken cancellationToken = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tag name is required.", nameof(name));
        if (name.Length > 80) throw new ArgumentException("Tag names must be 80 characters or fewer.", nameof(name));
        if (!IsHexColor(color)) throw new ArgumentException("Use a six-digit hex color such as #315C4C.", nameof(color));
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var tag = id is null
            ? await db.Tags.FirstOrDefaultAsync(existing => existing.Name.ToLower() == name.ToLower(), cancellationToken)
            : await db.Tags.FindAsync([id.Value], cancellationToken)
                ?? throw new KeyNotFoundException("The selected tag no longer exists.");
        if (tag is null)
        {
            tag = new Tag { Name = name, Color = color };
            db.Tags.Add(tag);
        }
        else
        {
            tag.Name = name;
            tag.Color = color;
        }
        await db.SaveChangesAsync(cancellationToken);
        return tag;
    }

    public async Task DeleteTagAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var tag = await db.Tags.FindAsync([id], cancellationToken);
        if (tag is null) return;
        db.Tags.Remove(tag);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsHexColor(string value) =>
        System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$");
}
