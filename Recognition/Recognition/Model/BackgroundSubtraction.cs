using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Recognition.Model
{
    public class BackgroundSubtraction
    {
        const int ObjectBit = 1;
        const int BackgroundBit = 0;

        public Bitmap SubtracteBackground(FastBitmap source, double range, FastBitmap background)
        {
            source.LockBits();
            var arr = Subtraction(source, range, background);
            source.UnlockBits();
            arr = Closing(arr);
            return ConvertIntsToBitmap(arr, source);
        }



        private int[,] Subtraction(FastBitmap current, double range, FastBitmap background)
        {
            int[,] result = new int[current.Width, current.Height];
            for (int x = 0; x < current.Width; x++)
            {
                for (int y = 0; y < current.Height; y++)
                {
                    System.Drawing.Color backgroundPixel = background.GetPixel(x, y);
                    System.Drawing.Color currentPixel = current.GetPixel(x, y);
                    double delta = Math.Abs(currentPixel.GetBrightness() - backgroundPixel.GetBrightness());
                    if (delta > range)
                        result[x, y] = ObjectBit;
                    else
                        result[x, y] = BackgroundBit;
                }
            }
            return result;
        }

        private double GetDeviation(FastBitmap bitmap, FastBitmap background)
        {
            double sum = 0;
            int count = bitmap.Height * bitmap.Width;

            for (int x = 0; x < bitmap.Width; x++)
                for (int y = 0; y < bitmap.Height; y++)
                {
                    System.Drawing.Color c = background.GetPixel(x, y);
                    sum += c.GetBrightness();
                }

            double expectation = sum / count;
            sum = 0;
            for (int x = 0; x < bitmap.Width; x++)
                for (int y = 0; y < bitmap.Height; y++)
                {
                    System.Drawing.Color c = background.GetPixel(x, y);
                    sum += (c.GetBrightness() - expectation) * (c.GetBrightness() - expectation);
                }
            double variance = sum / count;
            return Math.Sqrt(variance);
        }

        private Bitmap ConvertIntsToBitmap(int[,] map, FastBitmap frame)
        {
            Bitmap result = new Bitmap(map.GetLength(0), map.GetLength(1));
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    if (map[i, j] == ObjectBit)
                        result.SetPixel(i, j, frame.GetPixel(i, j));
                    else
                        result.SetPixel(i, j, Color.Black);
                }
            }
            return result;
        }

        private int[,] Closing(int[,] map)
        {
            int[,] result = new int[map.GetLength(0), map.GetLength(1)];
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    result[i, j] = SetPixel(map, i, j);
                }
            }
            return result;
        }

        private int SetPixel(int[,] map, int x, int y)
        {
            int[,] matrix = new int[,]
            {{0,1,0},
             {1,1,1},
             {0,1,0}
            };
            int result = 1;
            int xLen = matrix.GetLength(0);
            int yLen = matrix.GetLength(1);
            int[,] area = new int[xLen, yLen];
            int ai = 0, bi = 0;
            for (int i = x - xLen / 2; i <= x + xLen / 2; i++)
            {
                for (int j = y - yLen / 2; j <= y + yLen / 2; j++)
                {
                    if (i < 0 || i >= map.GetLength(0) || j < 0 || j >= map.GetLength(1))
                    {
                        area[ai, bi] = 0;
                    }
                    else
                        area[ai, bi] = map[i, j];
                    bi++;
                }
                bi = 0;
                ai++;
            }

            for (int i = 0; i < xLen && result == 1; i++)
            {
                for (int j = 0; j < yLen && result == 1; j++)
                {
                    if (matrix[i, j] == 1)
                        result &= matrix[i, j] & area[i, j];
                }
            }

            return result;
        }
    }
}
