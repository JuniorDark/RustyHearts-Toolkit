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
                    return Resources.ShopItemUnlimited;
                else
                    return string.Format(Resources.ShopItemItemAmountDays, itemAmount / 1440);
            }
            else
            {
                if (itemAmount == 0)
                    return Resources.ShopItemAmountDesc;
                else
                    return string.Format(Resources.ShopItemItemAmount, itemAmount);
            }
        }
        return Resources.ShopItemAmountDesc;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
