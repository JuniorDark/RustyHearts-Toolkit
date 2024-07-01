using System.Windows.Controls;

namespace RHToolkit.Utilities;

public class TabToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Check if the selected tab's is "Costume"
        if (value is TabItem tabItem && tabItem.Tag.ToString() == "Page")
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
