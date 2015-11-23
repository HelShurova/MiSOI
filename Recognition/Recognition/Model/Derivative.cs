using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recognition.Model
{
    class Derivative
    {
        public double X;
        public double Y;

        public static readonly int[,] Xmask = {{-1,-2,-1},
                                           {0,0,0},
                                           {1,2,1}};

        public static readonly int[,] Ymask = {{-1,0,1},
                                           {-2,0,2},
                                           {-1,0,1}};

        public Derivative(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Derivative()
        {
            X = Y = 0;
        }

        public Derivative(double[] region, int index, int width)
        {
            X = GetDerivative(region, index, width, Xmask);
            Y = GetDerivative(region, index, width, Ymask);
        }

        private double GetDerivative(double[] region, int index, int width, int[,] deriv)
        {
            double result = 0;
            int row = 0;
            for (int j = 0; j < deriv.GetLength(1); j++)
            {
                row += width;
                for (int i = 0; i < deriv.GetLength(0); i++)
                {
                    int currentIndex = row + index + i;
                    double val = region[currentIndex % region.Length];
                    if (val > 0.000001)
                        result += val * deriv[i, j];
                }
            }
            return result;
        }
    }
}
