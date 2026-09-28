using System.Threading;

namespace DailyLogAssistant.Services;

public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = @"Local\PersonalLogManager-Mutex";
    private const string EventName = @"Local\PersonalLogManager-Activate";
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _signal;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task? _listener;
    public bool IsPrimary { get; }
    public event EventHandler? ActivatePrimary;

    public SingleInstanceService()
    {
        _signal = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        _mutex = new Mutex(true, MutexName, out var created);
        IsPrimary = created;
        if (IsPrimary) _listener = Task.Run(WaitForActivation);
    }

    public void SignalPrimary()
    {
        try { using var signal = EventWaitHandle.OpenExisting(EventName); signal.Set(); }
        catch (WaitHandleCannotBeOpenedException) { }
    }

    private void WaitForActivation()
    {
        var handles = new[] { _signal, _stop.Token.WaitHandle };
        while (WaitHandle.WaitAny(handles) == 0)
        {
            ActivatePrimary?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener?.GetAwaiter().GetResult();
        _signal.Dispose();
        if (IsPrimary) _mutex.ReleaseMutex();
        _mutex.Dispose();
        _stop.Dispose();
    }
}
