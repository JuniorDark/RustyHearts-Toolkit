using RHToolkit.ViewModels.Controls;
using RHToolkit.ViewModels.Windows.Database.VM;

namespace RHToolkit.Utilities.Converters;

public class ItemDataViewModelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string indexString && int.TryParse(indexString, out int index))
        {
            if (value is List<ItemDataViewModel> itemDataViewModels)
            {
                var result = itemDataViewModels.FirstOrDefault(vm => vm.SlotIndex == index);
                return result ?? DependencyProperty.UnsetValue;
            }
            else if (value is ObservableCollection<InventoryItem> inventoryItems)
            {
                var result = inventoryItems.FirstOrDefault(item => item.SlotIndex == index)?.ItemDataViewModel;
                return result ?? DependencyProperty.UnsetValue;
            }
        }

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

