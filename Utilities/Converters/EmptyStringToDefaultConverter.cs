using System.Globalization;
using System.Windows.Data;

namespace RHGMTool.Utilities
{
    public class EmptyStringToDefaultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter is used only for one-way binding, so no need to implement Convert
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && string.IsNullOrEmpty(str))
            {
                // If the value is an empty string, return a default value (e.g., zero for integers)
                return targetType == typeof(int) ? 0 : value;
            }

            // If the value is not an empty string, return it as is
            return value;
        }
    }
}
