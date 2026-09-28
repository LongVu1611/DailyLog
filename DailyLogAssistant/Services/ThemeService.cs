using System.Windows;
using System.Windows.Media;
using DailyLogAssistant.Models;
using Microsoft.Win32;

namespace DailyLogAssistant.Services;

public sealed class ThemeService
{
    public void Apply(AppSettings settings)
    {
        var dark = settings.Theme switch
        {
            "Dark" => true,
            "Light" => false,
            _ => SystemParameters.HighContrast || IsSystemDark()
        };
        System.Windows.Application.Current.Resources["AccentBrush"] = new SolidColorBrush(ParseColor(settings.AccentColor));
        System.Windows.Application.Current.Resources["AppBackgroundBrush"] =
            new SolidColorBrush(dark ? System.Windows.Media.Color.FromRgb(32, 36, 34) : System.Windows.Media.Color.FromRgb(244, 246, 243));
        System.Windows.Application.Current.Resources["CardBackgroundBrush"] =
            new SolidColorBrush(dark ? System.Windows.Media.Color.FromRgb(47, 53, 50) : Colors.White);
        System.Windows.Application.Current.Resources["PrimaryTextBrush"] =
            new SolidColorBrush(dark ? System.Windows.Media.Color.FromRgb(238, 242, 239) : System.Windows.Media.Color.FromRgb(36, 53, 45));
    }

    private static System.Windows.Media.Color ParseColor(string value)
    {
        try { return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(value); }
        catch (FormatException) { return System.Windows.Media.Color.FromRgb(49, 92, 76); }
    }

    private static bool IsSystemDark()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int useLightTheme && useLightTheme == 0;
    }
}
