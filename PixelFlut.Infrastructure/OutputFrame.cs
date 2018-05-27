using System;
using System.Drawing;

namespace PixelFlut.Infrastructure
{
    public struct OutputFrame
    {
        public OutputFrame(int offsetX, int offsetY, OutputPixel[] pixels)
        {
            if (offsetX < 0 || offsetY < 0)
            {
                throw new ArgumentException("offset out of range!");
            }

            OffsetX = offsetX;
            OffsetY = offsetY;
            Pixels = pixels;
        }

        public readonly int OffsetX;
        public readonly int OffsetY;
        public readonly OutputPixel[] Pixels;
    }
}