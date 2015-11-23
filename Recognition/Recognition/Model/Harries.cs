using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

namespace Recognition.Model
{
    class Harries
    {
        const int RegionLength = 3;
        const int WindowWidth = 5;
        const int AutocorMatrixLength = 4;
        const int Diametr = 10;
        const double Coef = 0.04;//[0.04, 0.06]

        public List<Point> Corner(byte[] mask, int imgWidth, int imgHeight)
        {
            List<Point> result = new List<Point>();
            int imgLength = mask.Length;
            double[] brightness = new double[imgLength];
            for (int i = 0; i < mask.Length; i++)
            {
                brightness[i] = (double)mask[i]/255.0;
            }
            Dictionary<int, double[]> derivative = GetImgDerivative(brightness, imgWidth, imgHeight);
            Dictionary<int, double[]> m = AutocorMatrix(derivative, imgWidth,imgHeight, imgLength);
            Dictionary<int, double> responces = Responces(m, Coef);

            int[] localWindow = InitializeWindow(imgWidth, Diametr);
            int beforIndex = 0;
            foreach(int index in responces.Keys)
            {
                int delta = index - beforIndex;
                NextWindow(ref localWindow, imgLength,delta);
                Point localMax = LocalMax(localWindow, responces, imgWidth,imgHeight, Coef);
                    result.Add(localMax);
                beforIndex = index;
            }
            return result.Distinct().ToList();
        }


        private Point LocalMax(int[] window, Dictionary<int, double> responce,int imgWidth,int imgHeight, double range)
        {
            double max = -1;
            int maxIndex = 0;
            foreach(int index in window)
            {
                double value;
                if (responce.TryGetValue(index, out value))
                {
                    if (max < value)
                    {
                        max = value;
                        maxIndex = index;
                    }
                }                
            }
            return new Point(maxIndex % imgWidth, (maxIndex / imgWidth) % imgHeight);
        }

        private int[] InitializeWindow(int imgWidth, int windowSide)
        {
            int[] window = new int[windowSide * windowSide];
            int n= 0;
            for (int i = 0; i < windowSide; i++)
                for (int j = 0; j < windowSide; j++)
                {
                    int index = (j * imgWidth) + i;
                    window[n++] = index;
                }
            return window;
        }

        private Dictionary<int, double[]> AutocorMatrix(Dictionary<int, double[]> deriv, int imgWidth, int imgHeight, int imgLength)
        {
            var m = new Dictionary<int, double[]>();
            int[] window = InitializeWindow(imgWidth, WindowWidth);
            int beforIndex = 0;
            foreach(int index in deriv.Keys)
            {
                int delta = index - beforIndex;
                NextWindow(ref window, imgLength, delta);
                var s = Sum(window, deriv);
                if (s != null)
                {
                    m.Add(index, s);
                }
                beforIndex = index;
            }
            return m;
        }

        private void NextWindow(ref int[] window, int imgLength, int delta)
        {
            for (int i = 0; i < window.Length; i++)
                window[i] = (window[i] + delta) % imgLength;
        }

        private Dictionary<int, double> Responces(Dictionary<int, double[]> matrix, double k)
        {
            var result = new Dictionary<int, double>();
            foreach(int index in matrix.Keys)
            {
                double[] m = matrix[index];
                double det = m[0] * m[3] - m[1] * m[2];
                double tr = m[0] + m[3];
                double responce = det - (tr * tr * k);
                if (responce > k)
                    result.Add(index, responce);
            }
            return result;
        }

        private double[] Sum(int[] window, Dictionary<int, double[]> deriv)
        {
            double[] sum = new double[AutocorMatrixLength] {0,0,0,0};
            foreach (int index in window)
            {
                if (deriv.ContainsKey(index))
                {
                    double[] val = deriv[index];
                    for (int j = 0; j < AutocorMatrixLength; j++)
                    {
                        sum[j] += val[j];
                    }
                }
            }
            return sum;
        }

        private Dictionary<int, double[]> GetImgDerivative(double[] img, int width, int height)
        {
            var result = new Dictionary<int, double[]>();
            double range = 0.1;
            int rowIndex = 0;
            for (int j = 0; j < height - 2; j++)
            {
                for (int i = 0; i < width - 2; i++ )
                {
                    int centerIndex = rowIndex + width + i + 1;
                    double value = img[centerIndex];
                    if (value > range)
                    {
                        Derivative d = new Derivative(img, rowIndex + i, width);
                        if (Math.Abs(d.X) > range || Math.Abs(d.Y) > range)
                        {
                            var r = new double[AutocorMatrixLength];
                            r[0] = d.X * d.X * value;
                            r[1] = r[2] = d.X * d.Y * value;
                            r[3] = d.Y * d.Y * value;
                            if (r.Count(a=> Math.Abs(a) > range) >0)
                                result.Add(centerIndex, r);
                        }
                    }
                }
                rowIndex += width;
            }
            return result;
        }
    }
}
