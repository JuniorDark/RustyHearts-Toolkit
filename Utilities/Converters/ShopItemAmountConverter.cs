namespace RHToolkit.Utilities.Converters;

public class ShopItemAmountConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is int itemAmount && values[1] is int shopCategory)
        {
            if (shopCategory == 0)
            {
                if (itemAmount == 0)
                    return "1 Item(s) / Unlimited";
                else
                    return $"1 Item(s) / {itemAmount / 1440} Day(s)";
            }
            else
            {
                if (itemAmount == 0)
                    return "1 Item(s)";
                else
                    return $"{itemAmount} Item(s)";
            }
        }
        return "1 Item(s)";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
