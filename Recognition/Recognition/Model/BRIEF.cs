using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace Recognition.Model
{
    public class BRIEF
    {
        private const int TEST_COUNT = 256;
        public const double SIMILARITY_MIN_EDGE = 0.75;
        private List<BriefPair> testPairs = new List<BriefPair>();
        private Random random = new Random();
        public BRIEF(int pointNumber,int pointPos)
        {            
            for (int i = 0; i < pointNumber && i != pointPos && testPairs.Count < TEST_COUNT;++i )
            {
                testPairs.Add(new BriefPair() { FirstValue = pointPos, SecondValue = i });
            }
            while (testPairs.Count < TEST_COUNT)
            {
                BriefPair pair = new BriefPair { FirstValue = random.Next(pointNumber), SecondValue = random.Next(pointNumber) };
                if (!testPairs.Any(p => p.isEqual(pair)))
                    testPairs.Add(pair);
            }
        }

        public string GetImageDescriptor(List<Color> pixColors)
        {
            string res = "";
            foreach (BriefPair briefPair in testPairs)
            {
                res += (GetBrightness(pixColors[briefPair.FirstValue]) < GetBrightness(pixColors[briefPair.SecondValue])) ? "1" : "0";
            }
            return res;
        }

        private double GetBrightness(Color color)
        {
            return 0.3 * color.R + 0.59 * color.G + 0.11 * color.B;
        }

        public double GetDescriptorsSimilarity(string firstDesc, string secondDesc)
        {
            int length = Math.Min(firstDesc.Length, secondDesc.Length);
            int similCount = 0;
            for (int i = 0; i < length; ++i)
                if (firstDesc[i] == secondDesc[i])
                    ++similCount;
            return (double)similCount / (double)length;
        }
        private class BriefPair
        {
            public int FirstValue { get; set; }
            public int SecondValue { get; set; }

            public bool isEqual(BriefPair briefPair)
            {
                return (this.FirstValue == briefPair.FirstValue && this.SecondValue == briefPair.SecondValue) || (this.FirstValue == briefPair.SecondValue && this.SecondValue == briefPair.FirstValue);
            }
        }
    }

}
