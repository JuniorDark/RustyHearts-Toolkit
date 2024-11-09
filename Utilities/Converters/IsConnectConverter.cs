namespace RHToolkit.Utilities.Converters;

public class IsConnectConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? isConnect = value as string;
        if (isConnect == "Y")
            return Resources.Online;
        else if (isConnect == "N")
            return Resources.Offline;
        else
            return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

