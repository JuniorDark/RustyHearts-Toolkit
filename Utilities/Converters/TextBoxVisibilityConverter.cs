using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RHToolkit.Utilities
{
    public class TextBoxVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is bool isFocused && values[1] is string text)
            {
                if (isFocused || !string.IsNullOrEmpty(text))
                    return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
