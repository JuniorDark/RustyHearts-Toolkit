namespace RHToolkit.Utilities.Converters
{
    public class EnabledToSelectedIndexConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return -1; // Fallback value if values are not provided

            bool isEnabled = (bool)values[0];
            int selectedIndex = (int)values[1];

            // If ComboBox is not yet populated, return -1 as a fallback value
            if (selectedIndex == -1)
                return -1;

            // If IsEnabled is false, return 0 to set SelectedIndex to 0
            if (!isEnabled)
                return 0;

            return selectedIndex;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
