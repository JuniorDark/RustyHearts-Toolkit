namespace RHToolkit.Utilities.Converters
{
    public class ValueToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue && parameter != null)
            {
                string? param = parameter.ToString();
                if (param != null)
                {
                    string[] conditions = param.Split('|');

                    foreach (string condition in conditions)
                    {
                        if (int.TryParse(condition, out int conditionValue) && intValue == conditionValue)
                            return Visibility.Visible;
                    }
                }

                return Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
