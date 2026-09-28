using DailyLogAssistant.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Serilog;

namespace DailyLogAssistant.Services;

public sealed class DailyReminderService(
    LogService logs,
    SettingsService settings,
    IHostApplicationLifetime lifetime) : BackgroundService, IReminderService
{
    public event EventHandler? ReminderDue;
    private readonly SemaphoreSlim _checkLock = new(1, 1);
    private volatile bool _sessionLocked;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;
        try
        {
            await SafeCheckReminderAsync(stoppingToken);
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await SafeCheckReminderAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            _checkLock.Dispose();
        }
    }

    private async Task SafeCheckReminderAsync(CancellationToken cancellationToken)
    {
        try { await CheckReminderAsync(cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception) { Log.Error(exception, "Reminder check failed; the next scheduled check will retry"); }
    }

    public async Task CheckReminderAsync(CancellationToken cancellationToken = default)
    {
        if (!await _checkLock.WaitAsync(0, cancellationToken)) return;
        try
        {
            var now = DateTime.Now;
            var prefs = await settings.GetAsync(cancellationToken);
            var today = DateOnly.FromDateTime(now);
            if (_sessionLocked || !TimeOnly.TryParse(prefs.ReminderTime, out var reminderTime) ||
                !ReminderPolicy.IsDue(now, reminderTime, prefs.ReminderEnabled,
                    await logs.GetAsync(today, cancellationToken) is not null ||
                    (await logs.SearchAsync(new LogQuery(CategoryId: 1, From: today, To: today),
                        cancellationToken)).Count > 0,
                    prefs.DismissedDate, prefs.ReminderDate, prefs.SnoozeUntil))
                return;

            prefs.ReminderDate = today;
            prefs.SnoozeUntil = null;
            await settings.SaveAsync(prefs, cancellationToken);
            Log.Information("ReminderTriggered for {Date}", today);
            ReminderDue?.Invoke(this, EventArgs.Empty);
        }
        finally { _checkLock.Release(); }
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume) _ = SafeCheckReminderAsync(lifetime.ApplicationStopping);
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock) _sessionLocked = true;
        if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            _sessionLocked = false;
            _ = SafeCheckReminderAsync(lifetime.ApplicationStopping);
        }
    }
}
