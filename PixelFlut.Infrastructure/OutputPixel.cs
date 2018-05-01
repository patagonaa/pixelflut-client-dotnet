using System.Drawing;

namespace PixelFlut.Infrastructure
{
    public struct OutputPixel
    {
        public OutputPixel(Point point, Color color)
        {
            Point = point;
            Color = color;
        }

        public Point Point { get; }
        public Color Color { get; }
    }
}