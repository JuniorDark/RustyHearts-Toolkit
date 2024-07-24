namespace RHToolkit.Utilities.Converters;

public class ItemStateToImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        int state;
        if (value is int intValue)
        {
            state = intValue;
        }
        else if (value is string stringValue && int.TryParse(stringValue, out int parsedValue))
        {
            state = parsedValue;
        }
        else
        {
            return null;
        }

        string iconName = GetIconName(state);

        string imagePath = $"/Assets/images/cashshop/{iconName}.png";

        return imagePath;
    }

    private static string GetIconName(int state)
    {
        return state switch
        {
            0 => "lb_cashshop_none_01",
            1 => "lb_cashshop_sale_01",
            2 => "lb_cashshop_new_01",
            3 => "lb_cashshop_event_01",
            4 => "lb_cashshop_topseller_01",
            _ => "lb_cashshop_none_01",
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
