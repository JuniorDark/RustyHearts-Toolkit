using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RHToolkit.Utilities
{
    public class GreaterOrEqualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return DependencyProperty.UnsetValue;

            try
            {
                int valueToCompare = System.Convert.ToInt32(value);
                int parameterValue = System.Convert.ToInt32(parameter);

                return valueToCompare >= parameterValue;
            }
            catch (Exception)
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
