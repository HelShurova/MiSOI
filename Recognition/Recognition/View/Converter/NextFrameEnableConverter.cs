using System;
using System.Windows;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Data;


namespace Recognition.View.Converter
{
    [ValueConversion(typeof(int), typeof(bool))]
    class NextFrameEnableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (float)value >= 0 ? true : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
