using Microsoft.Win32;
using System.IO;
using System.Reflection;

namespace DailyLogAssistant.Services;

public sealed class AutoStartHelper
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "PersonalLogManager";

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKey);
        key.DeleteValue("DailyLogAssistant", throwOnMissingValue: false);
        if (enabled)
        {
            var executable = Environment.ProcessPath
                ?? throw new InvalidOperationException("Windows could not locate the application executable.");
            var command = Path.GetFileNameWithoutExtension(executable).Equals("dotnet",
                StringComparison.OrdinalIgnoreCase)
                ? $"\"{executable}\" \"{Path.Combine(AppContext.BaseDirectory, Assembly.GetEntryAssembly()?.GetName().Name + ".dll")}\""
                : $"\"{executable}\"";
            key.SetValue(AppName, command);
        }
        else
            key.DeleteValue(AppName, throwOnMissingValue: false);
    }
}
