using DailyLogAssistant.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyLogAssistant.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DailyLog> DailyLogs => Set<DailyLog>();
    public DbSet<AppSettings> Settings => Set<AppSettings>();
    public DbSet<LogEntry> Logs => Set<LogEntry>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<LogCategory> Categories => Set<LogCategory>();
    public DbSet<LogTag> LogTags => Set<LogTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyLog>(entity =>
        {
            entity.ToTable("DailyLogs");
            entity.HasKey(log => log.Id);
            entity.HasIndex(log => log.Date).IsUnique();
            entity.Property(log => log.Date).HasConversion(
                date => date.ToString("yyyy-MM-dd"),
                text => DateOnly.Parse(text));
            entity.Property(log => log.WorkDone).HasMaxLength(20000);
            entity.Property(log => log.Problems).HasMaxLength(20000);
            entity.Property(log => log.TomorrowPlan).HasMaxLength(20000);
            entity.Property(log => log.Notes).HasMaxLength(20000);
        });
        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.ToTable("AppSettings");
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.DismissedDate).HasConversion(
                date => date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null,
                text => text == null ? null : DateOnly.Parse(text));
            entity.Property(settings => settings.ReminderDate).HasConversion(
                date => date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null,
                text => text == null ? null : DateOnly.Parse(text));
        });
        modelBuilder.Entity<LogCategory>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(category => category.Id);
            entity.HasIndex(category => category.Name).IsUnique();
            entity.Property(category => category.Name).HasMaxLength(80);
            entity.Property(category => category.Icon).HasMaxLength(16);
            entity.Property(category => category.Color).HasMaxLength(16);
        });
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tags");
            entity.HasKey(tag => tag.Id);
            entity.HasIndex(tag => tag.Name).IsUnique();
            entity.Property(tag => tag.Name).HasMaxLength(80);
        });
        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.ToTable("Logs");
            entity.HasKey(log => log.Id);
            entity.HasIndex(log => log.Date);
            entity.HasIndex(log => log.CategoryId);
            entity.HasIndex(log => log.Status);
            entity.HasIndex(log => log.Title);
            entity.Property(log => log.Date).HasConversion(
                date => date.ToString("yyyy-MM-dd"),
                text => DateOnly.Parse(text));
            entity.HasOne(log => log.Category).WithMany(category => category.Logs)
                .HasForeignKey(log => log.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(log => log.Title).HasMaxLength(300);
        });
        modelBuilder.Entity<LogTag>(entity =>
        {
            entity.ToTable("LogTags");
            entity.HasKey(link => new { link.LogEntryId, link.TagId });
            entity.HasOne(link => link.Log).WithMany(log => log.LogTags)
                .HasForeignKey(link => link.LogEntryId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.Tag).WithMany(tag => tag.LogTags)
                .HasForeignKey(link => link.TagId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
