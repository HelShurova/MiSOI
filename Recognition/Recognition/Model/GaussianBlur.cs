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
        private int sigma;
        private int size;
        private double[] coefficients;
        private double sumCoef;

        public double[,] Filter(double[,] brightnesses)
        {
            sigma = 3;
            size = 3 * sigma;
              coefficients = new double[ size * 2 + 1];
              coefficients[size] = 1;
              for (int i = 0; i <= size; i++)
              {
                  coefficients[size + i] = Math.Exp((double)-i * i / 2 / sigma /sigma);
                  sumCoef += Math.Exp((double)-i * i / 2 / sigma / sigma);
                  coefficients[size - i] = coefficients[size + i];
              }

              int width = brightnesses.GetLength(0);  
            int height = brightnesses.GetLength(1);

            Parallel.For(0, height, j =>
            {
                double[] tmp = new double[width];
                for (int i = 0; i < width; i++)
                {
                    double sum = 0;
                    double newBright = 0;
                    int l = 0;
                    for (int k = -size; k <= size; k++)
                    {
                        l = i + k;
                        if (l >= 0 && l < width)
                        {
                            double value = brightnesses[l, j];
                            newBright = newBright + value * coefficients[k + size];
                            sum += coefficients[k + size];
                        }
                    }
                    tmp[i] = newBright;
                }
                for (int i = 0; i < width; i++)
                {
                    brightnesses[i, j] = tmp[i];
                }
            });
            Parallel.For(0, width, i =>
            {
                double[] tmp = new double[height];
                for (int j = 0; j < height; j++)
                {
                    double sum = 0;
                    double newBright = 0;

                    Color newPoint = Color.FromArgb(0, 0, 0);
                    int l = 0;
                    for (int k = -size; k <= size; k++)
                    {
                        l = j + k;
                        if (l >= 0 && l < height)
                        {
                            double value = brightnesses[i, l];
                            newBright = newBright + value * coefficients[k + size];
                            sum += coefficients[k + size];
                        }
                    }
                    tmp[j] = newBright;
                }
                for (int j = 0; j < height; j++)
                {
                    brightnesses[i, j] = tmp[j];
                }
            });
            return brightnesses;
        }

    }
}
