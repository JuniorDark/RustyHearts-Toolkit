using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace RHToolkit.Utilities
{
    using System;
    using System.Reflection;
    using System.Windows.Media.Imaging;

    public class BranchToImageSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value is not int branch)
            {
                return null;
            }

            string iconName = GetIconName(branch);

            string imagePath = $"pack://application:,,,/Assets/images/{iconName.ToLower()}.png";

            return new BitmapImage(new Uri(imagePath));
        }

        private static string GetIconName(int branch)
        {
            return branch switch
            {
                2 => "lb_icon_slot_02rare",
                4 => "lb_icon_slot_03unique",
                5 => "lb_icon_slot_01magic",
                6 => "lb_icon_slot_04epic",
                _ => "lb_icon_slot_00normal",
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }




}
