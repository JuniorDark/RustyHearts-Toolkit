using RHToolkit.ViewModels.Controls;

namespace RHToolkit.Utilities;

public class FrameViewModelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is List<FrameViewModel> frameViewModels && parameter is string indexString && int.TryParse(indexString, out int index))
        {
            var result = frameViewModels.FirstOrDefault(vm => vm.SlotIndex == index);
            return result ?? DependencyProperty.UnsetValue;
        }
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
