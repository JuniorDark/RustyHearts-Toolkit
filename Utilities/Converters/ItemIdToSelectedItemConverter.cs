using RHToolkit.Models;
using System.Windows.Controls;

namespace RHToolkit.Utilities
{
    public class ItemIdToSelectedItemConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value is not int)
                return null;

            int itemId = (int)value;

            if (parameter is not DataGrid dataGrid || dataGrid.Items.Count == 0)
                return null;

            // Find the item with the matching ID
            var selectedItem = dataGrid.Items.Cast<ItemData>().FirstOrDefault(item => item.ItemId == itemId);
            return selectedItem;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
