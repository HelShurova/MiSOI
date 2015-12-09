using System.Drawing;
using System;
using System.Linq;
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
using System.Diagnostics;
using Recognition.Model.Motion;
using System.Runtime.Serialization.Formatters.Binary;

namespace Recognition.ViewModel
{
    public class VideoViewModel : ViewModelBase
    {
        private const double TimerInterval = 0.2;
        private const int Scale = 6; //3

        private string _filePath;
        public string FilePath 
        { 
            get
            {
                return _filePath;
            }
            set
            {
                RealFrame = null;
                SubstarectedFrame = null;
                _filePath = value;
                RaisePropertyChanged("FileName");
                CurrentPosition = 0;
                FastBitmap background = GetBitmap(0);
                if (background != null)
                    _vibeModel.Initialize(background);                    
                CurrentPosition += (float)TimerInterval;
            }
        }

        public string FileName
        {
            get { return Path.GetFileName(FilePath); }
        }

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

        private FastBitmap _nextFrame = null;
        private byte[] _nextSubMask = null;
        private FastBitmap _nextSubFrame = null;

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

        private Harries _harries;
        private LucasKanadeMethod _lucasKanade;
        private VibeModel _vibeModel;

        public VideoViewModel()
        {
            CurrentPosition = -1;
            LoadCommand = new RelayCommand(Load);
            GetNextFrameCommand = new RelayCommand(GetNextFrame);
            _vibeModel = new VibeModel();
            _harries = new Harries();
            _lucasKanade = new LucasKanadeMethod();
        }

        private FastBitmap GetBitmap(float time)
        {
            FastBitmap result;
            using (var stream = new MemoryStream())
            {
                result = GetBitmap(time, stream);
            }
            return result;
        }

        private FastBitmap GetBitmap(float time, MemoryStream stream)
        {
            FastBitmap result = null;
            var ffMpeg = new NReco.VideoConverter.FFMpegConverter();
            ffMpeg.GetVideoThumbnail(FilePath, stream, time);
            if (stream.Length != 0)
            {
                Image img = Image.FromStream(stream);
                result = new FastBitmap(new Bitmap(img, img.Width/Scale , img.Height/Scale));
            }
            return result;
        }

        public RelayCommand LoadCommand { get; set; }
        public RelayCommand GetNextFrameCommand { get; set; }

        List<Motion> motions = new List<Motion>();
        List<Frame> curentFrames = new List<Frame>(); 
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

            //Эт надо мне пускай пока повисит
            //string[] filePaths = Directory.GetFiles(@".","*.dat");
            //for (int i = 0; i < filePaths.Length; i++)
            //{
            //    using (Stream stream = File.Open(filePaths[i], FileMode.Open))
            //    {
            //        BinaryFormatter bin = new BinaryFormatter();
            //        motions.Add((Motion)bin.Deserialize(stream));
            //    }
            //}



            //Сюда надо заглянуть после того как записали файлик(смотри указания ниже)))
            // Здесь меняешь имя файла и успех - должно хоть как-то заработать
            //Там оно не очень быстро работает - но в консольку пишется мусор(порядковый номер)- можно видеть,что хоть что-то рабоатает
            // Потом мусор этот можно убрать
            using (Stream stream = File.Open(@"test.test", FileMode.Open))
            {
                BinaryFormatter bin = new BinaryFormatter();
                Motion motion = (Motion)bin.Deserialize(stream);
                curentFrames = motion.Frames;
            }
            MotionSplitter splitter = new MotionSplitter();
            motions.AddRange(splitter.GetTrainigMotion());
            ActionDetection detection = new ActionDetection(motions, curentFrames);
            //Вот результат 
            string[] actionSequence = detection.getActionSequence();
        }

        private void GetNextFrame()
        {
            bool isNext = true;
            FastBitmap realFrame = null;
            byte[] subtractedMask = null;
            FastBitmap subFrame = null;
            using (MemoryStream stream = new MemoryStream())
            {
                if (CurrentPosition - TimerInterval < 0.0001)
                {
                    realFrame = GetBitmap(CurrentPosition, stream);
                    subtractedMask = _vibeModel.GetMask(realFrame);
                    subFrame = ApplyMask(subtractedMask, realFrame);
                }
                else
                {
                    realFrame = _nextFrame;
                    subtractedMask = _nextSubMask;
                    subFrame = _nextSubFrame;
                }
                isNext = GetFlow(subtractedMask, realFrame, subFrame, stream);
                if (!isNext)
                {
                    CurrentPosition = -1;
                    SubstarectedFrame = null;
                    _nextFrame = null;
                }
            }
        }

        List<Frame> frames = new List<Frame>();
        private bool GetFlow(byte[] subMask, FastBitmap realFrame,FastBitmap subFrame, MemoryStream stream)
        {
            ReturnImage image;
            bool isNext = true;
            List<Point> corners = _harries.Corner(subMask, subFrame.Width, subFrame.Height);
            if (corners.Count > 0)
            {
                _nextFrame = GetBitmap(CurrentPosition + (float)TimerInterval, stream);
                if (_nextFrame != null)
                {
                    _nextSubMask = _vibeModel.GetMask(_nextFrame);
                    _nextSubFrame = ApplyMask(_nextSubMask, _nextFrame);
                    image = _lucasKanade.GetImageWithDisplacement(subFrame, _nextSubFrame, corners);
                    SubstarectedFrame = image.bitmap.Source;
                    
                    // Вот это откомментируй в первый раз - оно запишет файлик - название пока надо менять вручную
                    // На счет названия может потом сделаю,чтобы было идентично с наванием видео
                    // Запускаешь прогу и покадрово прогоняешь(к сожалению долго, но я корч((( - пока так рабоатет)
                    // Длинное видео не получится - еще один пункт который буду смотреть - 75 кадров - у меня было это около 15 секунд- но если длиннеее видео 
                    // оно просто дальше 75 кадра считать не будет
                    // Потом перезапускаешь прогу и дальнейшие действия смотри коммент сверху
                    
                    //frames.Add(image.frame);
                    //if (frames.Count == 75)
                    //{
                    //    Motion motion = new Motion("test", frames);
                    //    BinaryFormatter binFormat = new BinaryFormatter();
                    //    // Сохранить объект в локальном файле.
                    //    using (Stream fStream = new FileStream("test.dat",
                    //       FileMode.Create, FileAccess.Write, FileShare.None))
                    //    {
                    //        binFormat.Serialize(fStream, motion);
                    //    }
                    //}
                    RealFrame = _nextFrame.Source;
                    CurrentPosition += (float)TimerInterval;
                }
                else
                    isNext = false;
            }
            return isNext;
        }

        private FastBitmap ApplyMask(byte[] mask, FastBitmap img)
        {
            Bitmap result = new Bitmap(img.Width, img.Height);
            FastBitmap fb = new FastBitmap(result);
            fb.LockBits();
            img.LockBits();
            int n = 0;
            for (int j = 0; j < img.Height; j++)
            {
                for (int i = 0; i < img.Width; i++)
                {
                    if (mask[n] > VibeModel.BackgroundByte)
                    {
                        fb.SetPixel(i, j, img.GetPixel(i, j));
                    }
                    n++;
                }
            }
            img.UnlockBits();
            fb.UnlockBits();
            return fb;
        }
    }
}
