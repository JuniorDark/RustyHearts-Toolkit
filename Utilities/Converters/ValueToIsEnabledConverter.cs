using System.Globalization;
using System.Windows.Data;

namespace RHGMTool.Utilities
{
    public class ValueToIsEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            int currentValue = System.Convert.ToInt32(value);
            var parameterString = parameter.ToString();

            if (parameterString == null)
                return false;

            var targetValues = parameterString.Split(',').Select(int.Parse);

            return targetValues.Contains(currentValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
