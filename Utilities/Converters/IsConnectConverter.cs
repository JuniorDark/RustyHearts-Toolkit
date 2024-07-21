namespace RHToolkit.Utilities.Converters;

public class IsConnectConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? isConnect = value as string;
        if (isConnect == "Y")
            return "Online";
        else if (isConnect == "N")
            return "Offline";
        else
            return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

