using System;
using System.Windows;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Data;


namespace Recognition.View.Converter
{
    [ValueConversion(typeof(string), typeof(bool))]
    class IsFileLoaded : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !string.IsNullOrEmpty((string)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
