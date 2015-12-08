using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;


//http://www.codeproject.com/Tips/240428/Work-with-bitmap-faster-with-Csharp
namespace Recognition.Model
{

    public class FastBitmap
    {
        public Bitmap Source { get; private set; }
        IntPtr _Iptr = IntPtr.Zero;
        BitmapData _bitmapData = null;

        public byte[] Pixels { get;private set; }
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int CCount { get { return Depth / 8; } }
        public PixelFormat PixelFormat { get; private set; }
        public int Stride { get; private set; }


        public FastBitmap(Bitmap source)
        {
            Width = source.Width;
            Height = source.Height;
            this.Source = source;
            PixelFormat = Source.PixelFormat;
            Depth = System.Drawing.Bitmap.GetPixelFormatSize(PixelFormat);
            _greyPixels = ToGrayscale();
        }

        private byte[] _greyPixels;

        public byte this[int x, int y]
        {
            get
            {
                return _greyPixels[y * Width + x];
            }
            set
            {
                _greyPixels[y * Width + x] = value;
            }
        }

        private byte[] ToGrayscale()
        {
            int bytesPerChannel = Bitmap.GetPixelFormatSize(Source.PixelFormat) / 8;
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            BitmapData colorData = Source.LockBits(rect, ImageLockMode.ReadOnly, Source.PixelFormat);
            Stride = colorData.Stride;
            IntPtr colorPointer = colorData.Scan0;
            byte[] pixels = new byte[colorData.Height * colorData.Width * bytesPerChannel];
            byte[] grayPixels = new byte[Width * Height];
            Marshal.Copy(colorPointer, pixels, 0, pixels.Length);
            Source.UnlockBits(colorData);
            int j = 0;
            for (int i = 0; i < pixels.Length; i += bytesPerChannel)
            {
                grayPixels[j++] = (byte)(0.299 * pixels[i + 2] + 0.587 * pixels[i + 1] + 0.114 * pixels[i]);
            }
            return grayPixels;
        }



        /// <summary>
        /// Lock bitmap data
        /// </summary>
        public void LockBits()
        {
            try
            {
                // get total locked pixels count
                int PixelCount = Width * Height;

                // Create rectangle to lock
                Rectangle rect = new Rectangle(0, 0, Width, Height);

                // Check if bpp (Bits Per Pixel) is 8, 24, or 32
                if (Depth != 8 && Depth != 24 && Depth != 32)
                {
                    throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                }

                // Lock bitmap and return bitmap data
                _bitmapData = Source.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat);

                // create byte array to copy pixel values
                int step = Depth / 8;
                Pixels = new byte[PixelCount * step];
                _Iptr = _bitmapData.Scan0;

                // Copy data from pointer to array
                Marshal.Copy(_Iptr, Pixels, 0, Pixels.Length);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Unlock bitmap data
        /// </summary>
        public void UnlockBits()
        {
            try
            {
                // Copy data from byte array to pointer
                Marshal.Copy(Pixels, 0, _Iptr, Pixels.Length);

                // Unlock bitmap data
                Source.UnlockBits(_bitmapData);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private int Index(int x, int y)
        { 
            return ((y * Width) + x) * CCount;
        }

        /// <summary>
        /// Get the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Color GetPixel(int x, int y)
        {
            Color clr = Color.Empty;

            int i = Index(x, y);

            if (i > Pixels.Length - CCount)
                throw new IndexOutOfRangeException();

            if (Depth == 32) // For 32 bpp get Red, Green, Blue and Alpha
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                byte a = Pixels[i + 3]; // a
                clr = Color.FromArgb(a, r, g, b);
            }
            if (Depth == 24) // For 24 bpp get Red, Green and Blue
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                clr = Color.FromArgb(r, g, b);
            }
            if (Depth == 8)
            // For 8 bpp get color value (Red, Green and Blue values are the same)
            {
                byte c = Pixels[i];
                clr = Color.FromArgb(c, c, c);
            }
            return clr;
        }

        /// <summary>
        /// Set the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void SetPixel(int x, int y, Color color)
        {
            int i = Index(x, y);

            if (Depth == 32) // For 32 bpp set Red, Green, Blue and Alpha
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
                Pixels[i + 3] = color.A;
            }
            if (Depth == 24) // For 24 bpp set Red, Green and Blue
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
            }
            if (Depth == 8)
            // For 8 bpp set color value (Red, Green and Blue values are the same)
            {
                Pixels[i] = color.B;
            }
        }
    }
}
