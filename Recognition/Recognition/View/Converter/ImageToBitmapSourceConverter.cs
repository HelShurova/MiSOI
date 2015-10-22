using System;
using System.Windows;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Data;

namespace Recognition.View.Converter
{
    [ValueConversion(typeof(Image), typeof(BitmapSource))]
    public class ImageToBitmapSourceConverter : IValueConverter
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            BitmapSource bitmapSource = null;
            if (value != null)
            {
                Image myImage = (Image)value;

                var bitmap = new Bitmap(myImage);
                IntPtr bmpPt = bitmap.GetHbitmap();
                bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                       bmpPt,
                       IntPtr.Zero,
                       Int32Rect.Empty,
                       BitmapSizeOptions.FromEmptyOptions());

                bitmapSource.Freeze();
                DeleteObject(bmpPt);
            }
            return bitmapSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
