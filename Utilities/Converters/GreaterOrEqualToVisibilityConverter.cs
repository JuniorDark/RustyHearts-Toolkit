namespace RHToolkit.Utilities
{
    public class GreaterOrEqualToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            if (value is int intValue && parameter is string paramString)
            {
                if (int.TryParse(paramString, out int threshold))
                {
                    return intValue >= threshold ? Visibility.Visible : Visibility.Collapsed;
                }
                else if (int.TryParse(value.ToString(), out int parsedValue))
                {
                    return parsedValue >= intValue ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            else if (value is string stringValue && int.TryParse(stringValue, out int parsedValue) && int.TryParse(parameter.ToString(), out int threshold))
            {
                return parsedValue >= threshold ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

}
