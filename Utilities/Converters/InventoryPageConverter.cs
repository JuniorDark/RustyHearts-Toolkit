using RHToolkit.ViewModels.Windows.Database.VM;

namespace RHToolkit.Utilities.Converters;

public class InventoryPageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ObservableCollection<InventoryItem> allItems && int.TryParse(parameter?.ToString(), out int pageNumber))
        {
            const int PageSize = 24; // Number of items per page
            int skip = (pageNumber - 1) * PageSize;
            return new ObservableCollection<InventoryItem>(allItems.Skip(skip).Take(PageSize));
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

