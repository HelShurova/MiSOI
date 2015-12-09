using System;
using System.IO;
using System.Timers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using Recognition.Model;
using Recognition.Model.Motion;
using System.Runtime.Serialization.Formatters.Binary;

namespace Recognition.ViewModel
{
    public class VideoViewModel : ViewModelBase
    {
        private Timer _timer;
        private const double TimerInterval = 0.2;
        private const int Scale = 6; //3
        private string[] _actionSequence = null;
        private int _actionIndex = 0;

        private bool _calculationComplete = false;
        public bool CalculationComplete
        {
            get { return _calculationComplete; }
            set {
                _timer.Enabled = value;
                _calculationComplete = value;
                RaisePropertyChanged("CalculationComplete");
            }
        }

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
                _actionIndex = 0;
                CalculationComplete = false;
            }
        }

        public string FileName
        {
            get { return Path.GetFileName(FilePath); }
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
            _actionIndex = 0;
            LoadCommand = new RelayCommand(Load);
            PlayCommand = new RelayCommand(Play);
            PauseCommand = new RelayCommand(Pause);
            StopCommand = new RelayCommand(Stop);
            ResumeCommand = new RelayCommand(Resume);

            _timer = new Timer(TimerInterval);
            _timer.Elapsed += OnTimedEvent;
            _timer.Enabled = false;
        }


        public RelayCommand LoadCommand { get; set; }
        public RelayCommand PlayCommand { get; set; }
        public RelayCommand PauseCommand { get; set; }
        public RelayCommand StopCommand { get; set; }
        public RelayCommand ResumeCommand { get; set; }

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
                MotionSplitter splitter = new MotionSplitter();

                var input = splitter.TestSplit(FilePath);

                motions.AddRange(splitter.GetMotion(MotionSplitter.TrainingMotionBin));
                ActionDetection detection = new ActionDetection(motions, input);
                //Вот результат 
                _actionSequence = detection.getActionSequence();
                CalculationComplete = true;
            }
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            MotionLabel = _actionSequence[_actionIndex];
            _actionIndex++;
            if (_actionIndex == _actionSequence.Length)
            {
                _actionIndex = 0;
                _timer.Enabled = false;
            }
        }

        private void Pause() 
        {
            _timer.Enabled = false;
        }
        private void Play() 
        {
            _actionIndex = 0;
            _timer.Enabled = true;
        }
        private void Stop()
        {
            _timer.Enabled = false;
            _actionIndex = 0;
        }
        private void Resume() 
        {
            _timer.Enabled = true;
        }

    }
}
