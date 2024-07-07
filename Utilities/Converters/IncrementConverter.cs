namespace RHToolkit.Utilities
{
    public class IncrementConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            int maxValue = System.Convert.ToInt32(value);

            if (maxValue >= 10000)
                return 100;
            else if (maxValue >= 1000)
                return 10;
            else if (maxValue >= 100)
                return 1;
            else
                return 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
