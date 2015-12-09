using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;

namespace Recognition.Model.Motion
{
    public class MotionSplitter
    {
        public const string TrainingMotionBin = @"..\..\Resources\TrainigMotion\motions.bin";
        const string TrainingMotionVideo = @"..\..\Resources\TrainigMotion\training (online-video-cutter.com).mp4";
        const string TrainingBackground = @"..\..\Resources\TrainigMotion\background.jpg";
        const double TimerInterval = 0.2;
        const int DescriptorCount = 5;
        const int Scale = 6;
        private FastBitmap _nextFrame = null;
        private byte[] _nextSubMask = null;
        private FastBitmap _nextSubFrame = null;
        private Harries _harries;
        private LucasKanadeMethod _lucasKanade;
        private VibeModel _vibeModel;
        private float _currentPosition = 0;

        string[] MotionLabel = new string[] {"Рука к полке", "Взять","Поставить", "Рука от полки", "Присесть", "Встать"};

        public MotionSplitter()
        {
            _vibeModel = new VibeModel();
            _harries = new Harries();
            _lucasKanade = new LucasKanadeMethod();
        }

        private void InitBackgroundByImage(string file)
        {
            Image img = Image.FromFile(file);
            var fb = new FastBitmap(new Bitmap(img, img.Width / Scale, img.Height / Scale));
            _vibeModel.Initialize(fb);
        }

        //public void SaveBackgroundPicture(string videoPath, string backgroundPath)
        //{
        //    using (MemoryStream stream = new MemoryStream())
        //    {
        //        var ffMpeg = new NReco.VideoConverter.FFMpegConverter();
        //        ffMpeg.GetVideoThumbnail(videoPath, stream, 0);
        //        if (stream.Length != 0)
        //        {
        //            Image img = Image.FromStream(stream);
        //            img.Save(backgroundPath);
        //        }
        //    }
        //}

        private FastBitmap GetBitmap(float time, MemoryStream stream, string file)
        {
            FastBitmap result = null;
            var ffMpeg = new NReco.VideoConverter.FFMpegConverter();
            ffMpeg.GetVideoThumbnail(file, stream, time);
            if (stream.Length != 0)
            {
                Image img = Image.FromStream(stream);
                result = new FastBitmap(new Bitmap(img, img.Width / Scale, img.Height / Scale));
            }
            return result;
        }

        public List<Motion> TrainingSplit()
        {
            _currentPosition = 0;
            List<Motion> motions = new List<Motion>();
            InitBackgroundByImage(TrainingBackground);
            var frames = Split(TrainingMotionVideo);
            int labelIndex = 0;
            int descriptionIndex = 0;
            List<Frame> motion = new List<Frame>();
            foreach(Frame f in frames)
            {
                motion.Add(f);
                descriptionIndex++;
                if (descriptionIndex == DescriptorCount)
                {
                    descriptionIndex = 0;
                    motions.Add(new Motion(MotionLabel[labelIndex], motion));
                    labelIndex++;
                    motion = new List<Frame>();
                }
            }
            return motions;
        }

        public List<Frame> TestSplit(string file)
        {
            _currentPosition = 0;
            using (MemoryStream stream = new MemoryStream())
            {
                var bitmap = GetBitmap(_currentPosition, stream, file);
                _vibeModel.Initialize(bitmap);
            }
            List<Frame> frames = Split(file);
            return frames;
        }

        public List<Frame> Split(string file)
        {
            List<Frame> frames = new List<Frame>();
            bool isNext = true;
            while (isNext)
            {
                FastBitmap realFrame = null;
                byte[] subtractedMask = null;
                FastBitmap subFrame = null;
                using (MemoryStream stream = new MemoryStream())
                {
                    if (_currentPosition - TimerInterval < 0.0001)
                    {
                        realFrame = GetBitmap(_currentPosition, stream, file);
                        subtractedMask = _vibeModel.GetMask(realFrame);
                        subFrame = ApplyMask(subtractedMask, realFrame);
                    }
                    else
                    {
                        realFrame = _nextFrame;
                        subtractedMask = _nextSubMask;
                        subFrame = _nextSubFrame;
                    }
                    Frame frame;
                    isNext = GetFlow(subtractedMask, realFrame, subFrame, stream,file, out frame);
                    frames.Add(frame);
                }
            }
            return frames;
        }

        private void SerializeMotions(List<Motion> motions, string file)
        {
            
            using (Stream stream = File.Open(file, FileMode.Create))
            {
                BinaryFormatter bin = new BinaryFormatter();
                bin.Serialize(stream, motions);
            }
        }

        

        public List<Motion> GetMotion(string file)
        {
            List<Motion> result = null;
            using (Stream stream = File.Open(file, FileMode.Open))
            {
                BinaryFormatter bin = new BinaryFormatter();
                result = (List<Motion>)bin.Deserialize(stream);
            }
            return result;
        }

        private bool GetFlow(byte[] subMask, FastBitmap realFrame, FastBitmap subFrame, MemoryStream stream, string file, out Frame frame)
        {
            Frame localFrame =null;
            bool isNext = true;
            List<Point> corners = _harries.Corner(subMask, subFrame.Width, subFrame.Height);
            if (corners.Count > 0)
            {
                _nextFrame = GetBitmap(_currentPosition + (float)TimerInterval, stream, file);
                if (_nextFrame != null)
                {
                    _nextSubMask = _vibeModel.GetMask(_nextFrame);
                    _nextSubFrame = ApplyMask(_nextSubMask, _nextFrame);
                    localFrame = _lucasKanade.GetImageWithDisplacement(subFrame, _nextSubFrame, corners).frame;
                    _currentPosition += (float)TimerInterval;
                }
                else
                    isNext = false;
            }
            frame = localFrame;
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
