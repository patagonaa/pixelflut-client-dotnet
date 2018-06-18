using System;

namespace PixelFlut.Infrastructure
{
    public struct OutputFrame
    {
        public OutputFrame(int offsetX, int offsetY, OutputPixel[] pixels, int cacheId = -1, bool offsetStatic = false)
        {
            if (offsetX < 0 || offsetY < 0)
            {
                throw new ArgumentException("offset out of range!");
            }

            OffsetX = offsetX;
            OffsetY = offsetY;
            Pixels = pixels;
            CacheId = cacheId;
            OffsetStatic = offsetStatic;
        }

        public readonly int OffsetX;
        public readonly int OffsetY;
        public readonly OutputPixel[] Pixels;
        public readonly int CacheId;
        public readonly bool OffsetStatic;
    }
}