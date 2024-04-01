﻿using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RHGMTool.Utilities
{
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString)
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
            }

            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
