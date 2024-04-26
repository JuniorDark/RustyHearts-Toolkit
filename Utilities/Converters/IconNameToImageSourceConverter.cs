using System.Windows.Media.Imaging;

namespace RHToolkit.Utilities
{
    public class IconNameToImageSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? iconName = value as string;

            if (string.IsNullOrEmpty(iconName))
            {
                return null;
            }

            string? imagePath = FindImage(iconName);

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                BitmapImage bitmapImage = new();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(imagePath);
                bitmapImage.EndInit();
                return bitmapImage;
            }
            else
            {
                return new BitmapImage(new Uri("pack://application:,,,/Assets/images/question_icon.png"));
            }
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
