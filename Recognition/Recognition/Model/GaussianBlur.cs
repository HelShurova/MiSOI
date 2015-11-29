using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recognition.Model
{
    class GaussianBlur
    {
        //static int maxWin = 15;
        //private const int K = 2;
        //private const double SIGMA = 0.55;
        private int sigma;
        private int size;
        private double[] coefficients;
        private double sumCoef;

        public FastBitmap Filter(FastBitmap bitmap)
        {
            sigma = 3;
            size = 3 * sigma;
            //coefficients = CountCoefficients(sigma, size);

//            int N = (int)Math.Ceiling(3 * sigma);
              coefficients = new double[ size * 2 + 1];
              coefficients[size] = 1;
              for (int i = 0; i <= size; i++)
              {
                  coefficients[size + i] = Math.Exp((double)-i * i / 2 / sigma /sigma);
                  sumCoef += Math.Exp((double)-i * i / 2 / sigma / sigma);
                  coefficients[size - i] = coefficients[size + i];
              }


            //double[,] buffer = new double[bitmap.Width, bitmap.Height];
            //Parallel.For(0, bitmap.Height, j =>
            //{
            //    for (int i = size; i < bitmap.Width - size; i++)
            //    {
            //        double pix = 0;
            //        for (int k = -size; k < size; k++)
            //        {
            //            pix += bitmap[i + k, j] * coefficients[k + size];
            //        }
            //        buffer[i, j] = pix / sumCoef;
            //    }
            //});

            //Parallel.For(0, bitmap.Width, i =>
            //{
            //    for (int j = size; j < bitmap.Height - size; j++)
            //    {
            //        double pix = 0;
            //        for (int k = -size; k < size; k++)
            //        {
            //            pix += buffer[i, j + k] * coefficients[k + size];
            //        }
            //        bitmap[(int)i, j] = (byte)(pix / sumCoef);
            //    }
            //});






     //      // bitmap.LockBits();
     //       int maxDim = bitmap.Width;
            //Color[] tmp = new Color[bitmap.Width];
     //       double s2 = 2 * sigma * sigma;
     //       int N = (int)Math.Ceiling(3 * sigma);
     //       double[] window = new double[ N * 2 + 1];
     //       int maxWin = N * 2;
     //       window[N] = 1;
     //       for (int i = 0; i <= N; i++)
     //       {
     //           window[N + i] = Math.Exp(-i * i / s2);
     //           window[N - i] = window[N + i];
     //       }

            Parallel.For(0, bitmap.Height, j =>
            {
                Color[] tmp = new Color[bitmap.Width];
                for (int i = 0; i < bitmap.Width; i++)
                {
                    double sum = 0;
                    Color newPoint = Color.FromArgb(0, 0, 0);
                    double newPointR = 0;
                    double newPointG = 0;
                    double newPointB = 0;
                    int l = 0;
                    //Color[] temp = new Color[size * 2 + 1];
                    for (int k = -size; k <= size; k++)
                    {
                        l = i + k;
                        if (l >= 0 && l < bitmap.Width)
                        {
                            Color p = bitmap.GetPixel(l, j);
                            newPointR = newPointR + p.R * coefficients[k + size];
                            newPointG = newPointG + p.G * coefficients[k + size];
                            newPointB = newPointB + p.B * coefficients[k + size];
                            //newPoint = Color.FromArgb(
                            //    (int)(Math.Min(255, )),
                            //    (int)(Math.Min(255, )),
                            //    (int)(Math.Min(255, )));
                            sum += coefficients[k + size];
                            //temp[k + size] = newPoint;
                        }
                    }
                    newPoint = Color.FromArgb(
                            (int)(newPointR / sum),
                            (int)(newPointG / sum),
                            (int)(newPointB / sum));
                    tmp[i] = newPoint;
                }
                for (int i = 0; i < bitmap.Width; i++)
                {
                    bitmap.SetPixel(i, j, tmp[i]);
                }
            });
            Parallel.For(0, bitmap.Width, i =>
            {
                Color[] tmp = new Color[bitmap.Height];
                for (int j = 0; j < bitmap.Height; j++)
                {
                    double sum = 0;
                    double newPointR = 0;
                    double newPointG = 0;
                    double newPointB = 0;

                    Color newPoint = Color.FromArgb(0, 0, 0);
                    int l = 0;
                    for (int k = -size; k <= size; k++)
                    {
                        l = j + k;
                        if (l >= 0 && l < bitmap.Height)
                        {
                            Color p = bitmap.GetPixel(i, l);
                            newPointR = newPointR + p.R * coefficients[k + size];
                            newPointG = newPointG + p.G * coefficients[k + size];
                            newPointB = newPointB + p.B * coefficients[k + size];

                            //newPoint = Color.FromArgb(
                            //    (int)(Math.Min(255, newPoint.R + p.R * coefficients[k + size])),
                            //    (int)(Math.Min(255, newPoint.G + p.G * coefficients[k + size])),
                            //    (int)(Math.Min(255, newPoint.B + p.B * coefficients[k + size])));
                            sum += coefficients[k + size];
                        }
                    }
                    newPoint = Color.FromArgb(
                            (int)(newPointR / sum),
                            (int)(newPointG / sum),
                            (int)(newPointB / sum));
                    tmp[j] = newPoint;
                }
                for (int j = 0; j < bitmap.Height; j++)
                {
                    bitmap.SetPixel(i, j, tmp[j]);
                }
            });
     ////       bitmap.UnlockBits();
            return bitmap;
        }

    }
}
