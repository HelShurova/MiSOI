using System.Drawing;
using System;
using NReco.VideoConverter;
using System.IO;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using DirectShowLib;
using DirectShowLib.DES;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using Recognition.Model;

namespace Recognition.ViewModel
{
    public class VideoViewModel : ViewModelBase
    {
        // White pixel - object, black - background
        const int ObjectBit = 1;
        const int BackgroundBit = 0;
        const int Scale = 5; 
        private const double TimerInterval = 1;
        private const double Range = 0.2;

        private string _filePath;
        public string FilePath 
        { 
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;
                RaisePropertyChanged("FileName");
                CurrentPosition = 0;
                Background = GetBitmap(0);
            }
        }
        public string FileName
        {
            get { return Path.GetFileName(FilePath); }
        }
        public Bitmap Background { get; set; }

        private Bitmap _realFrame;
        public Bitmap RealFrame
        {
            get
            {
                return _realFrame;
            }

            set
            {
                _realFrame = value;
                RaisePropertyChanged("RealFrame");
            }
        }

        private Bitmap _subtractedFrame;
        public Bitmap SubstarectedFrame
        {
            get
            {
                return _subtractedFrame;
            }
            set
            {
                _subtractedFrame = value;
                RaisePropertyChanged("SubstarectedFrame");
            }
        }
        private float _currentPosition = -1;
        public float CurrentPosition
        {
            get
            { return _currentPosition; }
            private set
            {
                _currentPosition = value;
                RaisePropertyChanged("CurrentPosition");
            }
        }

        private Harries harries;
        
        public VideoViewModel()
        {
            CurrentPosition = -1;
            LoadCommand = new RelayCommand(Load);
            GetNextFrameCommand = new RelayCommand(GetNextFrame);
            harries = new Harries();
        }

        private double GetDeviation(Bitmap bitmap)
        {
            double sum = 0;
            int count = bitmap.Height * bitmap.Width;

            for (int x = 0; x < bitmap.Width; x++)
                for (int y = 0; y < bitmap.Height; y++)
                {
                    System.Drawing.Color c = Background.GetPixel(x,y);
                    sum += c.GetBrightness();
                }

            double expectation = sum/count;
            sum = 0;
            for (int x = 0; x < bitmap.Width; x++)
                for (int y = 0; y < bitmap.Height; y++)
                {
                    System.Drawing.Color c = Background.GetPixel(x, y);
                    sum += (c.GetBrightness() - expectation)*(c.GetBrightness() - expectation);
                }
            double variance = sum/count;
            return Math.Sqrt(variance);
        }

        private int[,] Subtraction(Bitmap current, double range)
        {
            int[,] result = new int[current.Width, current.Height];
            for (int x =0; x< current.Width; x++)
            {
                for(int y = 0; y < current.Height; y++)
                {
                    System.Drawing.Color backgroundPixel = Background.GetPixel(x, y);
                    System.Drawing.Color currentPixel = current.GetPixel(x, y);
                    double delta = Math.Abs(currentPixel.GetBrightness() - backgroundPixel.GetBrightness());
                    if (delta > range)
                        result[x,y] = ObjectBit;
                    else
                        result[x,y] = BackgroundBit;
                }
            }
            return result;
        }
        private Bitmap ConvertIntsToBitmap(int[,] map)
        {
            Bitmap result = new Bitmap(map.GetLength(0), map.GetLength(1));
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    if (map[i, j] == ObjectBit)
                        result.SetPixel(i, j, RealFrame.GetPixel(i,j));
                    else
                        result.SetPixel(i, j, Color.Black);
                }
            }
            return result;
        }

        private int[,] Closing(int[,] map)
        {            
            int[,] result = new int[map.GetLength(0), map.GetLength(1)];
            for (int i = 0; i < map.GetLength(0); i++ )
            {
                for (int j = 0 ; j < map.GetLength(1); j++)
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
                for (int j = y - yLen/2; j <= y + yLen/2; j++)
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

            for (int i = 0 ; i < xLen && result == 1; i++)
            {
                for (int j = 0 ; j < yLen && result == 1; j++)
                {
                    if (matrix[i, j] == 1)
                        result &= matrix[i, j] & area[i, j];
                }
            }

            return result;
        }
        private Bitmap GetBitmap(float time)
        {
            Bitmap result;
            using (var stream = new MemoryStream())
            {
                result = GetBitmap(time, stream);
            }
            return result;
        }

        private Bitmap GetBitmap(float time, MemoryStream stream)
        {
            Bitmap result = null;
            var ffMpeg = new NReco.VideoConverter.FFMpegConverter();
            ffMpeg.GetVideoThumbnail(FilePath, stream, time);
            if (stream.Length != 0)
            {
                Image img = Image.FromStream(stream);
                result = new Bitmap(img, img.Width / Scale, img.Height / Scale);
            }
            return result;
        }

        public RelayCommand LoadCommand { get; set; }
        public RelayCommand GetNextFrameCommand { get; set; }

        private void Load()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.InitialDirectory = @"Resources";
            dlg.DefaultExt = ".mp4";
            dlg.Filter = "Video files |*.avi; *.mp4; ";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                FilePath = dlg.FileName;
            }
        }

        private void GetNextFrame()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                RealFrame = GetBitmap(CurrentPosition, stream);
                if (RealFrame != null)
                {
                    var arr = Subtraction(RealFrame, Range);
                    arr = Closing(arr);
                    Bitmap frame = ConvertIntsToBitmap(arr);
                    List<Point> corner = harries.Corner(frame);
                    foreach(Point c in corner)
                    {
                        for (int x = c.X - 2; x < c.X + 2; x++)
                            for (int y = c.Y - 2; y < c.Y + 2; y++ )
                                frame.SetPixel(x,y, Color.Green);
                    }
                    SubstarectedFrame = frame;
                    CurrentPosition += (float)TimerInterval;
                }
                else
                {
                    CurrentPosition = -1;
                }
            }
        }

        //TODO: save all images in directories, then try show it as video
    }
}
