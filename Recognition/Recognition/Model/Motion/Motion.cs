using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace Recognition.Model.Motion
{
    [Serializable()]
    public class Frame
    {
        public Frame(double[,] xp, double[,] xn, double[,] yp, double[,] yn)
        {
            XP = xp;
            XN = xn;
            YP = yp;
            YN = yn;
        }

        public double[,] XP {get; set;}
        public double[,] XN { get; set; }
        public double[,] YP { get; set; }
        public double[,] YN { get; set; }
    }

    [Serializable()]
    public class Motion
    {
        public string Label { get; set; }
        public List<Frame> Frames { get; set; }

        public Motion(string label, List<Frame> frames)
        {
            Label = label;
            Frames = frames;
        }
    }
}
