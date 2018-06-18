using PixelFlut.Infrastructure;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PixelFlut.Demo.Filters
{
    class PixelDiffFilter : IFilter
    {
        private const int KeyFrameAfterFrames = 20;
        private OutputPixel[] lastFrame;
        private int frameCount;

        public OutputFrame ApplyFilter(OutputFrame frame)
        {
            var pixels = OptimizeBandwidth(frame.Pixels, ref this.lastFrame);
            this.frameCount++;
            return new OutputFrame(frame.OffsetX, frame.OffsetY, pixels, frame.CacheId, frame.OffsetStatic);
        }

        private OutputPixel[] OptimizeBandwidth(OutputPixel[] outputPixels, ref OutputPixel[] lastFrame)
        {
            if (lastFrame == null)
            {
                lastFrame = outputPixels;
                return outputPixels;
            }

            var toReturn = new List<OutputPixel>(100000);

            var pixelLength = outputPixels.Length;
            for (int i = 0; i < pixelLength; i++)
            {
                var oldPixel = lastFrame[i];
                var newPixel = outputPixels[i];

                if (!(this.frameCount % KeyFrameAfterFrames == 0) && ColorsEqual(oldPixel, newPixel))
                    continue;
                lastFrame[i] = newPixel;
                toReturn.Add(newPixel);
            }

            return toReturn.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ColorsEqual(OutputPixel oldPixel, OutputPixel newPixel)
        {
            var c1 = oldPixel.Color;
            var c2 = newPixel.Color;

            if (c1 == c2)
                return true;

            var diff = (c1 & 0xFF) - (c2 & 0xFF);
            if (diff > 10 || diff < -10)
                return false;
            c1 >>= 8;
            c2 >>= 8;

            diff = (c1 & 0xFF) - (c2 & 0xFF);
            if (diff > 10 || diff < -10)
                return false;
            c1 >>= 8;
            c2 >>= 8;

            diff = (c1 & 0xFF) - (c2 & 0xFF);
            if (diff > 10 || diff < -10)
                return false;

            return true;
        }

        public void Dispose()
        {
            this.lastFrame = null;
        }
    }
}
