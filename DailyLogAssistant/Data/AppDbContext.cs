using DailyLogAssistant.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyLogAssistant.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DailyLog> DailyLogs => Set<DailyLog>();
    public DbSet<AppSettings> Settings => Set<AppSettings>();

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
            entity.ToTable("Settings");
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.DismissedDate).HasConversion(
                date => date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null,
                text => text == null ? null : DateOnly.Parse(text));
            entity.Property(settings => settings.ReminderDate).HasConversion(
                date => date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null,
                text => text == null ? null : DateOnly.Parse(text));
        });
    }
}
