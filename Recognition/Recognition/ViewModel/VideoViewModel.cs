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
                _filePath = value;
                RaisePropertyChanged("FilePath");
                RaisePropertyChanged("FileName");
                CurrentPosition = 0;                    
                CurrentPosition += (float)TimerInterval;
            }
        }

        public string FileName
        {
            get { return Path.GetFileName(FilePath); }
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

        private string _motionLabel;
        public string MotionLabel
        {
            get { return _motionLabel; }
            set
            {
                _motionLabel = value;
                RaisePropertyChanged("MotionLabel");
            }
        }

        public VideoViewModel()
        {
            CurrentPosition = -1;
            LoadCommand = new RelayCommand(Load);
        }


        public RelayCommand LoadCommand { get; set; }
        public RelayCommand PlayCommand { get; set; }
        public RelayCommand PauseCommand { get; set; }
        public RelayCommand StopCommand { get; set; }

        List<Motion> motions = new List<Motion>();
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



            //MotionSplitter splitter = new MotionSplitter();

            //var input = splitter.TestSplit(FilePath);

            //motions.AddRange(splitter.GetMotion(MotionSplitter.TrainingMotionBin));
            //ActionDetection detection = new ActionDetection(motions, input);
            ////Вот результат 
            //string[] actionSequence = detection.getActionSequence();
        }

    }
}
