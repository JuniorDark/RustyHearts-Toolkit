using Wpf.Ui.Appearance;

namespace RHToolkit.Utilities;

internal sealed class ThemeToIndexConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ApplicationTheme.Light)
        {
            return 1;
        }

        return 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is 0)
        {
            return ApplicationTheme.Dark;
        }

        return ApplicationTheme.Light;
    }
}
