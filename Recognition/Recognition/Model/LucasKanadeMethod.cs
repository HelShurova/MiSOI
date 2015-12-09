using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Concurrent;
using Recognition.Model.Motion;

namespace Recognition.Model
{
    public class ReturnImage
    {
        public Frame frame {get; set;}
        public FastBitmap bitmap{ get; set;}

        public ReturnImage(Frame frame, FastBitmap bitmap)
        {
            this.frame = frame;
            this.bitmap = bitmap;
        }

    }
    public class LucasKanadeMethod
    {
        private const int POINT_WINDOW_SIDE = 8;
        private const int MAX_DISPLACEMENT = 150;
        private const int MAX_KOEFF = 300;

        private int _nBytesPerPixel;
        private BRIEF _brief;

        public LucasKanadeMethod()
        {
        }


        public ReturnImage GetImageWithDisplacement(FastBitmap currFrame, FastBitmap nextFrame, List<Point> edgePoints)
        {
            _nBytesPerPixel = currFrame.CCount;
            currFrame.LockBits();
            nextFrame.LockBits();
            byte[] currRgbValues = currFrame.Pixels;
            byte[] nextRgbValues = nextFrame.Pixels;
            ConcurrentDictionary<Point, Point> displacements = new ConcurrentDictionary<Point, Point>();
            
            Parallel.ForEach(edgePoints, (edgePoint) =>
            {
                int edgePointPos;
                List<Color> pointVicinity = GetPointVicinity(edgePoint, currRgbValues, currFrame.Width, currFrame.Height, currFrame.Stride, out edgePointPos);
                _brief = new BRIEF((POINT_WINDOW_SIDE + 1) * (POINT_WINDOW_SIDE + 1), edgePointPos);
                string vicinityDescriptor = _brief.GetImageDescriptor(pointVicinity);
                if (pointVicinity[edgePointPos] != Color.Black)
                {
                    Point nextFramePoint = FindPointDiscplacement(edgePoint, nextRgbValues, nextFrame.Width, nextFrame.Height, vicinityDescriptor, nextFrame.Stride, pointVicinity[edgePointPos]);
                    displacements.TryAdd(edgePoint, nextFramePoint);
                }
                else
                    displacements.TryAdd(edgePoint, edgePoint);
            });

            currFrame.UnlockBits();
            nextFrame.UnlockBits();

            Frame frame = GetFrameByChanell(currFrame, displacements);
            //frames.Add();
            ReturnImage image = new ReturnImage(frame, currFrame);
            return image;
        }

        private Frame GetFrameByChanell(FastBitmap source, ConcurrentDictionary<Point, Point> displacements)
        {
            Pen pen = new Pen(Color.Aqua);
            pen.EndCap = LineCap.ArrowAnchor;
            RemoveLongLine(displacements);

            Bitmap bmp = new Bitmap(source.Width, source.Height);
            using (Graphics graph = Graphics.FromImage(bmp))
            {
                graph.FillRectangle(Brushes.White, new Rectangle(0, 0, source.Width, source.Height));
            }
            double[,] chanelXP = new double[source.Width, source.Height];
            double[,] chanelXM = new double[source.Width, source.Height];
            double[,] chanelYP = new double[source.Width, source.Height];
            double[,] chanelYM = new double[source.Width, source.Height];

            Graphics graphics = Graphics.FromImage(source.Source);

            foreach (var displacement in displacements)
            {
                int differenceX = displacement.Value.X - displacement.Key.X,
                    differenceY = displacement.Value.Y - displacement.Key.Y;
                bool isPosX = true, isPosY = true;
                if (differenceX < 0)
                    isPosX = false;
                if (differenceY < 0)
                    isPosY = false;

                differenceX = (int)(255 * (1 - Math.Min(1, Math.Abs((double)differenceX / 10))));
                differenceY = (int)(255 * (1 - Math.Min(1, Math.Abs((double)differenceY / 10))));

                double brightnessXColor = Color.FromArgb(differenceX, differenceX, differenceX).GetBrightness();
                double brightnessYColor = Color.FromArgb(differenceY, differenceY, differenceY).GetBrightness();

                for (int y = displacement.Key.Y - 1; y <= displacement.Key.Y + 1; ++y)
                    for (int x = displacement.Key.X - 1; x <= displacement.Key.X + 1; ++x)
                    {
                        if (isPosX)
                        {
                            chanelXP[x, y] = brightnessXColor;
                        }
                        else
                        {
                            //differenceX = Math.Abs(differenceX);
                            chanelXM[x, y] = brightnessXColor;
                        }
                        if (isPosY)
                        {
                            chanelYM[x, y] = brightnessYColor;
                        }
                        else
                        {
                            //differenceY = Math.Abs(differenceY);
                            chanelYP[x, y] = brightnessYColor;
                        }
                    }
                if (displacement.Key.X == displacement.Value.X && displacement.Key.Y == displacement.Value.Y)
                    graphics.DrawLine(pen, displacement.Key, new Point(displacement.Value.X + 2, displacement.Value.Y + 2));
                else
                    graphics.DrawLine(pen, displacement.Key, displacement.Value);
            }
            GaussianBlur blur = new GaussianBlur();
            Frame frame = new Frame(blur.Filter(chanelXP), blur.Filter(chanelXM), blur.Filter(chanelYP), blur.Filter(chanelYM));

            return frame;
        }

        //can be deleted: it was good idea for angel video
        private void RemoveLongLine(ConcurrentDictionary<Point, Point> displacements)
        {
            for (int i = 0; i < displacements.Keys.Count;++i )
            {
                if (Math.Abs(displacements.Keys.ElementAt(i).X - displacements[displacements.Keys.ElementAt(i)].X) > MAX_DISPLACEMENT ||
                    Math.Abs(displacements.Keys.ElementAt(i).Y - displacements[displacements.Keys.ElementAt(i)].Y) > MAX_DISPLACEMENT)
                {
                    displacements[displacements.Keys.ElementAt(i)] = displacements.Keys.ElementAt(i);
                }
            }
        }

        private Point FindPointDiscplacement(Point sourcePoint, byte[] rgbValues, int imgWidth, int imgHeight, string currFrameDescr, int imgStride, Color edgePointColor)
        {
            Point result = new Point(sourcePoint.X, sourcePoint.Y);
            int koeff = 1;
            Dictionary<Point, double> similarities = new Dictionary<Point, double>();
            while (koeff < MAX_KOEFF)
            {
                int[,] pixelEdges = new int[,] { { -1 * koeff, -1 * koeff, -1 * koeff, 0, 0, 1 * koeff, 1 * koeff, 1 * koeff }, { -1 * koeff, 0, 1 * koeff, -1 * koeff, 1 * koeff, -1 * koeff, 0, 1 * koeff } };
                int length = pixelEdges.GetLength(1);
                int pntBehindImg = 0;
                for (int i = 0; i < length; ++i)
                {
                    int newX = sourcePoint.X + pixelEdges[0, i], newY = sourcePoint.Y + pixelEdges[1, i];
                    if (newX >= imgWidth || newX < 0 || newY >= imgHeight || newY < 0)
                    {
                        ++pntBehindImg;
                        break;
                    }
                    Point newPoint = new Point(newX, newY);
                    int pointPos;
                    List<Color> pointVicinity = GetPointVicinity(newPoint, rgbValues, imgWidth, imgHeight, imgStride,out pointPos);
                    if (pointVicinity.Any(c => c != Color.Black) )
                    {
                        string nextFrameDescr = _brief.GetImageDescriptor(pointVicinity);
                        if (!similarities.Keys.Contains(newPoint))
                            similarities.Add(newPoint, _brief.GetDescriptorsSimilarity(currFrameDescr, nextFrameDescr));
                    }
                }
                if (pntBehindImg == length)
                    break;
                ++koeff;
            }
            if (similarities.Count > 0)
            {
                Point similarPoint = similarities.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                double asd = similarities[similarPoint];
                if (similarities[similarPoint] > BRIEF.SIMILARITY_MIN_EDGE)
                    result = similarPoint;
            }
            return result;
        }



        private List<Color> GetPointVicinity(Point point, byte[] rgbValues, int imgWidth, int imgHeight, int imgStride,out int pointPos)
        {
            pointPos = 0;
            List<Color> result = new List<Color>();
            int leftOffset = Math.Min(POINT_WINDOW_SIDE / 2, point.X),
                rightOffset = Math.Min(POINT_WINDOW_SIDE / 2, imgWidth - point.X - 1),
                topOffset = Math.Min(POINT_WINDOW_SIDE / 2, point.Y),
                bottomOffset = Math.Min(POINT_WINDOW_SIDE / 2, imgHeight - point.Y - 1);
            if (rightOffset + leftOffset != POINT_WINDOW_SIDE)
                if (rightOffset < POINT_WINDOW_SIDE / 2)
                    leftOffset = POINT_WINDOW_SIDE - rightOffset;
                else
                    rightOffset = POINT_WINDOW_SIDE - leftOffset;
            if (topOffset + bottomOffset != POINT_WINDOW_SIDE)
                if (topOffset < POINT_WINDOW_SIDE / 2)
                    bottomOffset = POINT_WINDOW_SIDE - topOffset;
                else
                    topOffset = POINT_WINDOW_SIDE - bottomOffset;
            for (int y = point.Y - topOffset; y <= point.Y + bottomOffset; ++y)
                for (int x = point.X - leftOffset; x <= point.X + rightOffset; ++x)
                {
                    if (point.X == x && point.Y == y)
                        pointPos = result.Count;
                    int bytePosition = imgStride * y + x * _nBytesPerPixel;
                    result.Add(Color.FromArgb(rgbValues[bytePosition + 2], rgbValues[bytePosition + 1], rgbValues[bytePosition]));
                }
            return result;
        }

    }
}
