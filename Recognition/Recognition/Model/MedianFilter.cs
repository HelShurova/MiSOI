using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Recognition.Model
{
    public class MedianFilter
    {
        public void Filter(FastBitmap bitmap, int radius)
        {
            Parallel.For (0, bitmap.Width, x=>
                {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    List<int> list = new List<int>();
                    for (int i = x - radius; i <= x + radius; i++)
                    {
                        for (int j = y - radius; j <= y + radius; j++)
                        {
                            if (i < 0 || j < 0 || i >= bitmap.Width || j >= bitmap.Height)
                                list.Add(Color.Black.ToArgb());
                            else
                                list.Add(bitmap.GetPixel(x, y).ToArgb());
                        }
                    }
                    list.Sort();
                    bitmap.SetPixel(x, y, Color.FromArgb(list[list.Count() / 2]));
                }
            }
            );
            
        }
    }
}
