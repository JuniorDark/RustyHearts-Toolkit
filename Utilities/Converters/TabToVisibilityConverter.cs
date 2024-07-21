using System.Windows.Controls;

namespace RHToolkit.Utilities.Converters;

public class TabToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TabItem tabItem && tabItem.Tag != null && tabItem.Tag.ToString() == "Page")
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

