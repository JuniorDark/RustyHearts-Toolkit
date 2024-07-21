using static RHToolkit.Models.EnumService;

namespace RHToolkit.Utilities.Converters;

public class CharacterClassConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null && Enum.IsDefined(typeof(CharClass), value))
        {
            CharClass classValue = (CharClass)value;
            return GetEnumDescription(classValue);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

