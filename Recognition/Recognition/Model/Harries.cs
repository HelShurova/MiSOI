using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Recognition.Model
{
    class Harries
    {
        const int RegionLength = 3;
        const int WindowWidth = 5;
        Point NotExist = new Point(-1, -1);
        const double Coef = 0.06;//[0.04, 0.06]

        public List<Point> Corner(Bitmap img)
        {
            FastBitmap fb = new FastBitmap(img);
            fb.LockBits();
            List<Point> result = new List<Point>();
            Derivative[,] imgDerivative = GetImgDerivative(fb);
            double[,] responceMatrix = new double[img.Width, img.Height];
            for (int x = 0; x < img.Width - WindowWidth; x++)
                for (int y = 0; y < img.Height - WindowWidth; y++ )
                {
                    Point[,] window = CreateWindow(x, y);
                    if (IsObjectInWindow(window, fb))
                    {
                        Matrix autoCorrelationMatrix = GetAutocorrelationMatrix(fb, window, imgDerivative);
                        responceMatrix[x + 2, y + 2] = Response(autoCorrelationMatrix, Coef);
                    }
                }
            int radius = 10;
            for (int i = radius; i < img.Width - radius; i+=2*radius)
                for (int j = radius; j < img.Height - radius; j+=2*radius )
                {
                    Point localMax = LocalMax(new Point(i, j), radius, responceMatrix, Coef);
                    if (localMax != NotExist)
                        result.Add(localMax);
                }
            fb.UnlockBits();
            return result;
        }

        private bool IsObjectInWindow(Point[,] window, FastBitmap img)
        {
            bool result = false;
            foreach(Point p in window)
            {
                if (img.GetPixel(p.X, p.Y) != Color.Black)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        private Point LocalMax(Point center, int radius, double[,] response, double range)
        {
            Point result = NotExist;
            double max = -1;
            Point localMax = new Point();
            for (int i = center.X - radius; i <= center.X + radius; i++)
                for (int j = center.Y - radius; j <= center.Y + radius; j++ )
                {
                    if (response[i,j] > max)
                    {
                        max = response[i, j];
                        localMax.X = i;
                        localMax.Y = j;
                    }
                }
            if (max > range)
                result = localMax;
            return result;
        }

        private Point[,] CreateWindow(int x, int y)
        {
            Point[,] window = new Point[WindowWidth , WindowWidth];
            for (int i = 0; i < WindowWidth; i++)
                for (int j = 0; j < WindowWidth; j++)
                {
                    window[i, j].X = x + i;
                    window[i, j].Y = y + j;
                }
            return window;
        }

        private Matrix GetAutocorrelationMatrix(FastBitmap img, Point[,] window, Derivative[,] ImgDerivative)
        {
            Matrix result = new Matrix(2, 2);
            foreach (Point p in window)
            {
                double[,] m = new double[2, 2];
                Derivative I = ImgDerivative[p.X,p.Y];
                if (I == null)
                    I = new Derivative(0, 0);
                m[0, 0] = I.X * I.X;
                m[0, 1] = m[1, 0] = I.X * I.Y;
                m[1, 1] = I.Y * I.Y;
                Matrix adder = new Matrix(m);
                result += adder;
            }
            return result;
        }

        private double Response(Matrix m, double k)
        {
            double det = m.Determinant().Re;
            double mT = m.Trace().Re;
            return det - mT * mT * k;
        }

        private Derivative[,] GetImgDerivative(FastBitmap img)
        {
            Derivative[,] result = new Derivative[img.Width, img.Height];
            double[,] region = new double[RegionLength, RegionLength];
            for (int x = 1; x < img.Width - 1; x++)
            {
                for (int y = 1; y < img.Height - 1; y++)
                {
                    region = GetRegion(img, x, y);
                    result[x, y] = new Derivative(region);
                }
            }
            return result;
        }

        private double[,] GetRegion(FastBitmap img, int x, int y)
        {
            double[,] region = new double[RegionLength, RegionLength];
            for (int i = 0; i < RegionLength; i++)
                for (int j = 0; j < RegionLength; j++)
                {
                    region[i, j] = img.GetPixel(i + x - 1, j + y - 1).GetBrightness();
                }
            return region;
        }
    }
}
