using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recognition.Model
{
    class ActionDetection
    {
        List<Motion> motions;

        public ActionDetection()
        {

        }

        public double Correlation(byte[] A, byte[] B)
        {
            int lA = A.GetLength(0);
            int lB = B.GetLength(0);
            int N = lA;

            // First calculate the sum of elements of matrices.
            // Then calculate multiplication of corresponding elements.
            // Also calculate sum of squares.
            ulong sumA = 0;
            ulong sumB = 0;
            ulong sumMultAB = 0;
            ulong sumSquaresA = 0;
            ulong sumSquaresB = 0;
            for (int i = 0; i < N; i++)
            {
                sumA += A[i];
                sumB += B[i];
                sumMultAB += (ulong)A[i] * B[i];
                sumSquaresA += (ulong)Math.Pow(A[i], 2);
                sumSquaresB += (ulong)Math.Pow(B[i], 2);
            }

            ulong numerator = ((ulong)N * sumMultAB) - (sumA * sumB);
            double denominator = Math.Pow((((ulong)N * sumSquaresA) - Math.Pow(sumA, 2)) * (((ulong)N * sumSquaresB) - Math.Pow(sumB, 2)), 0.5);
            return (numerator / denominator);
        }
    }
}
