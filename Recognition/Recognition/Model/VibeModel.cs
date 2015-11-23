using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Recognition.Model
{
    class Pixel
    {
        public byte[] Samples;

        public Pixel(byte[] samples)
        {
            Samples = samples;
        }
    }

    class VibeModel
    {
        public const byte BackgroundByte = 0;
        const int NumberOfSamples = 15;
        const int MatchingThreshold = 70;
        const int MatchingNumber = 2;
        const int UpdateFactor = 5;
        const int Radius = 2;

        Pixel[] _pixels;
        int _width;
        int _height;
        private Random _r;

        public void Initialize(FastBitmap img)
        {
            _r = new Random();
            img.LockBits();
            _height = img.Height;
            _width = img.Width;

            int capacity = _width * _height;

            _pixels = new Pixel[capacity];

            int n = 0;
            for (int j = 0; j < _height; j++)
            {
                for (int i = 0 ; i < _width; i++)
                {
                    List<byte> samples = new List<byte>();
                    samples.Add(img[i,j]);
                    byte[] randomPixels = GetRandomGreyPixels(img, i, j);
                    samples.AddRange(randomPixels);
                    _pixels[n] = new Pixel(samples.ToArray());
                    n++;
                }
            }
            img.UnlockBits();
        }
        
        private byte[] GetRandomGreyPixels (FastBitmap img, int x, int y)
        {    
            byte[] result = new byte[NumberOfSamples - 1];
            for (int i = 0; i < NumberOfSamples - 1; i++ )
            {
                GetCoordinate(ref x, ref y);
                result[i] = img[x, y];
            }
            return result;
        }

        private void GetCoordinate(ref int x,ref int y)
        {
            int topY = y - Radius;
            int leftX = x - Radius;
            int d = 2 * Radius;
            y = GetCoordinate(topY, _height - 1, d);
            x = GetCoordinate(leftX, _width - 1, d);
        }

        private int GetCoordinate(int coordinate, int maxValue, int diametr)
        {
            int seed = _r.Next(diametr);
            int result = coordinate + seed;
            if (result > maxValue)
                result = maxValue;
            if (result < 0)
                result = 0;
            return result;
        }

        private int Compare(byte pixel_data, Pixel pixel)
        {
            int matchingCount = 0;
            for (int i = 0 ; i < NumberOfSamples; i++)
            {
                int sub = Math.Abs(pixel_data - pixel.Samples[i]);
                if (sub < MatchingThreshold)
                {
                    matchingCount++;
                    if (matchingCount >= MatchingNumber)
                        return 1;
                }
            }
            return 0;
        }

        private void UpdateModel(byte pixel_data, int x, int y)
        {
            if (_r.Next(NumberOfSamples) == UpdateFactor)
            {
                UpdatePixel(x, y, pixel_data);
                GetCoordinate(ref x, ref y);
                UpdatePixel(x, y, pixel_data);
            }
        }

        private void UpdatePixel(int x, int y, byte pixel_data)
        {
            int randomIndexOfSamle = _r.Next(NumberOfSamples);
            _pixels[y * _width + x].Samples[randomIndexOfSamle] = pixel_data;
        }

        public byte[] GetMask(FastBitmap img)
        {
            byte[] mask = new byte[_height * _width];
            int n = 0;
            for (int j = 0; j < _height; j++)
            {
                for (int i = 0; i < _width; i++)
                {
                    byte pixel_data = img[i,j];
                    if (Compare(pixel_data, _pixels[n]) == 1)
                    {
                        mask[n] = BackgroundByte;
                        UpdateModel(pixel_data, i,j);
                    }
                    else
                    {
                        mask[n] = pixel_data;
                    }
                    n++;
                }
            }          
            return mask;
        }
    }
}
