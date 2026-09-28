using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DailyLogAssistant.Views;

public sealed class DateOnlyDateTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DateOnly date ? date.ToDateTime(TimeOnly.MinValue) : DependencyProperty.UnsetValue;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DateTime date ? DateOnly.FromDateTime(date) : System.Windows.Data.Binding.DoNothing;
}
