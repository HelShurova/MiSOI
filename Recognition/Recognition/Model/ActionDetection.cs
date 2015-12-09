using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recognition.Model.Motion;
using System.Windows;
using System.ComponentModel;
using System.Windows.Threading;

namespace Recognition.Model
{
    class ActionDetection
    {
        List<Recognition.Model.Motion.Motion> motions;
        List<Frame> frames;

        public ActionDetection(List<Recognition.Model.Motion.Motion> motions, List<Frame> frames)
        {
            this.motions = motions;
            this.frames = frames;
        }

        private object ExitFrame(object f)
        {
            ((DispatcherFrame)f).Continue = false;
            return null;
        }

        public int[,] calculateMatrix()
        {
            int[,] matrix = new int[motions.Count*5, frames.Count];
            int countFrame = 0;
            foreach(Frame frame in frames){
                if (frame != null)
                {
                    int countMotion = 0;
                    foreach (Recognition.Model.Motion.Motion motion in motions)
                    {
                        foreach (Frame motionFrame in motion.Frames)
                        {
                            double correlationFrame = Correlation(TwoDToOneD(frame.XN), TwoDToOneD(motionFrame.XN))
                                                    + Correlation(TwoDToOneD(frame.XP), TwoDToOneD(motionFrame.XP))
                                                    + Correlation(TwoDToOneD(frame.YN), TwoDToOneD(motionFrame.YN))
                                                    + Correlation(TwoDToOneD(frame.YP), TwoDToOneD(motionFrame.YP));

                            matrix[countMotion, countFrame] = Convert.ToInt32(correlationFrame / 4 * 255);
                            countMotion++;
                        }
                        DispatcherFrame frameB = new DispatcherFrame();
                        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), frameB);
                        Dispatcher.PushFrame(frameB);
                    }
                    countFrame++;
                    //if (countFrame == 40)
                    //    return matrix;
                    Console.Write(countFrame);
                }
            }
            return matrix;
        }

        public int[,] buildMotionMatrix(int[,] matrix)
        {
            int[,] matrixMotions = new int[motions.Count, frames.Count];
            double[,] diagonal = new double[5, 5] { { 1, 0.4, 0.2, 0, 0 }, { 0.4, 1, 0.4, 0.2, 0 }, { 0.2, 0.4, 1, 0.4, 0.2 }, { 0, 0.2, 0.4, 1, 0.4 }, { 0, 0, 0.2, 0.4, 1 } };
            for (int i = 0; i < matrix.GetLength(0) - 4; i += 5)
            {
                for (int j = 0; j < matrix.GetLength(1) - 4; j++)
                {
                    for (int inneri = i; inneri < i + 5; inneri++)
                    {
                        for (int innerj = j; innerj < j + 5; innerj++)
                        {
                            matrixMotions[(int)(i / 5), j] += (int)(matrix[inneri, innerj] * diagonal[inneri - i, innerj - j]);
                        }
                    }
                    Console.Write(matrix[i, j] + "\t");
                }
                Console.WriteLine();
            }
            Console.WriteLine(); Console.WriteLine("----------------------------------------------------");
            Console.WriteLine(); Console.WriteLine();
            for (int i = 0; i < matrixMotions.GetLength(0); i++)
            {
                for (int j = 0; j < matrixMotions.GetLength(1); j++)
                {
                    Console.Write(matrixMotions[i, j] + "\t");
                }
                Console.WriteLine();
            }
            return matrixMotions;
        }

        public string[] getActionSequence()
        {
            int[,] matrix = calculateMatrix();
            int[,] matrixMotions = buildMotionMatrix(matrix);
            string[] actionSequence = new string[frames.Count];
            for (int j = 0; j < matrixMotions.GetLength(1); j++)
            {
                int max = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (matrixMotions[i, j] > max && matrixMotions[i, j] > 10)
                    {
                        actionSequence[j] = motions[i].Label;
                        max = matrixMotions[i, j];
                    }
                }
                if (actionSequence[j] == "")
                {
                    actionSequence[j] = "Не определено";
                }
                Console.Write(actionSequence[j] + "\t");
            }
            return actionSequence;
        }


        private byte[] TwoDToOneD(double[,] income)
        {
            double[] resultDouble = income.Cast<double>().ToArray();
            byte[] result = new byte[income.LongLength];
            for (int i = 0; i < income.LongLength; i++)
            {
                result[i] = Convert.ToByte(resultDouble[i]);
            }
            return result;
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
            ulong numerator = 0;
            if (((ulong)N * sumMultAB) > (sumA * sumB))
                numerator = ((ulong)N * sumMultAB) - (sumA * sumB);
            double denominator = Math.Pow((((ulong)N * sumSquaresA) - Math.Pow(sumA, 2)) * (((ulong)N * sumSquaresB) - Math.Pow(sumB, 2)), 0.5);

            //ulong numerator = 0;
            //numerator = (sumA * sumB);
            //double denominator = Math.Pow((sumSquaresA * sumSquaresB), 0.5);

            if (denominator == 0)
                return numerator;
            return (numerator / denominator);
        }
    }
}
