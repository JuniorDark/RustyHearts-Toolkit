using System.Windows.Media.Imaging;

namespace RHToolkit.Utilities.Converters;

public class IconNameToImageSourceMultiConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] == null || values[1] == null)
            return null;

        string? iconName = values[0] as string;
        bool isEnabled = (bool)values[1];

        if (string.IsNullOrEmpty(iconName) && isEnabled)
        {
            return null;
        }
        else if (string.IsNullOrEmpty(iconName) && !isEnabled)
        {
            return LoadImageFromResources("pack://application:,,,/Assets/images/icon_closed.png");
        }

        string? imagePath = FindImage(iconName);

        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            return LoadImageFromResources(imagePath);
        }
        else
        {
            return LoadImageFromResources("pack://application:,,,/Assets/images/question_icon.png");
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static string? FindImage(string? iconName)
    {
        if (string.IsNullOrEmpty(iconName))
        {
            return null;
        }

        string[] files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, iconName.ToLower() + ".png", SearchOption.AllDirectories);

        if (files.Length > 0)
        {
            return files[0];
        }
        else
        {
            return null;
        }
    }

    private static BitmapImage LoadImageFromResources(string uri)
    {
        BitmapImage bitmapImage = new();
        bitmapImage.BeginInit();
        bitmapImage.UriSource = new Uri(uri, UriKind.RelativeOrAbsolute);
        bitmapImage.EndInit();
        return bitmapImage;
    }
}


