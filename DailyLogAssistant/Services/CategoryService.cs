using DailyLogAssistant.Data;
using DailyLogAssistant.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyLogAssistant.Services;

public sealed class CategoryService(IDbContextFactory<AppDbContext> factory) : ICategoryService
{
    public async Task<List<LogCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Categories.AsNoTracking().OrderByDescending(category => category.IsBuiltIn)
            .ThenBy(category => category.Name).ToListAsync(cancellationToken);
    }

    public async Task<LogCategory> CreateCustomCategoryAsync(
        string name, string icon, string color, CancellationToken cancellationToken = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.", nameof(name));
        if (name.Length > 80) throw new ArgumentException("Category names must be 80 characters or fewer.", nameof(name));
        if (string.IsNullOrWhiteSpace(icon) || icon.Length > 16)
            throw new ArgumentException("Provide a short category icon.", nameof(icon));
        if (!System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9A-Fa-f]{6}$"))
            throw new ArgumentException("Use a six-digit hex color such as #315C4C.", nameof(color));
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (await db.Categories.AnyAsync(category => category.Name.ToLower() == name.ToLower(), cancellationToken))
            throw new InvalidOperationException("A category with that name already exists.");
        var category = new LogCategory { Name = name, Icon = icon, Color = color, IsBuiltIn = false };
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task DeleteCustomCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var category = await db.Categories.Include(item => item.Logs)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("The selected category no longer exists.");
        if (category.IsBuiltIn) throw new InvalidOperationException("Built-in categories cannot be deleted.");
        if (category.Logs.Count > 0) throw new InvalidOperationException("Move or delete this category's logs before deleting it.");
        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
    }
}
