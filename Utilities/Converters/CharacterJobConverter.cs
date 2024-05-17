using static RHToolkit.Models.EnumService;

namespace RHToolkit.Utilities;

public class CharacterJobConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] != null && values[1] != null &&
            Enum.IsDefined(typeof(CharClass), values[0]))
        {
            CharClass charClass = (CharClass)values[0];
            int job = System.Convert.ToInt32(values[1]);

            Enum jobEnum = GetJobEnum(charClass, job);

            return GetEnumDescription(jobEnum);
        }
        return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

