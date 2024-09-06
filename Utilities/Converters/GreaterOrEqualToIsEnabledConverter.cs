namespace RHToolkit.Utilities.Converters
{
    public class GreaterOrEqualToIsEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            // Convert value and parameter to numbers
            if (double.TryParse(value.ToString(), out double numberValue) &&
                double.TryParse(parameter.ToString(), out double numberParameter))
            {
                // Return true if value is greater than or equal to the parameter, false otherwise
                return numberValue >= numberParameter && numberValue != 0;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
