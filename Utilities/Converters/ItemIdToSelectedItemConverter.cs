using RHToolkit.Models;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace RHToolkit.Utilities
{
    public class ItemIdToSelectedItemConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is int))
                return null;

            int itemId = (int)value;

            var dataGrid = parameter as DataGrid;
            if (dataGrid == null || dataGrid.Items.Count == 0)
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
