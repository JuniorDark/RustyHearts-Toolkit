namespace RHToolkit.Utilities.Converters
{
    public class DropItemCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double fDropItemCount)
            {
                // Multiply FDropItemCount by 100
                return fDropItemCount * 100;
            }
            else if (value is int nDropItemCount)
            {
                // Divide NDropItemCount by 1000
                return (double)nDropItemCount / 1000;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
