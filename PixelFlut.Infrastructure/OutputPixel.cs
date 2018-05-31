using System.Drawing;

namespace PixelFlut.Infrastructure
{
    public struct OutputPixel
    {
        public OutputPixel(int x, int y, int argb)
        {
            X = x;
            Y = y;
            Color = argb;
        }

        public OutputPixel(Point point, Color color)
        {
            X = point.X;
            Y = point.Y;
            Color = color.ToArgb();
        }

        public int X;
        public int Y;
        public int Color;
    }
}