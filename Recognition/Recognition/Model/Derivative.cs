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

        public Derivative(double[,] region)
        {
            X = GetDerivative(region, Xmask);
            Y = GetDerivative(region, Ymask);
        }

        private double GetDerivative(double[,] region, int[,] deriv)
        {
            double result = 0;
            for (int i = 0; i < deriv.GetLength(0); i++)
            {
                for (int j = 0; j < deriv.GetLength(1); j++)
                {
                    result += region[i, j] * deriv[i, j];
                }
            }
            return result;
        }
    }
}
