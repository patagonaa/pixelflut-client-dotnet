using PixelFlut.Infrastructure;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PixelFlut.Demo.Filters
{
    class PixelDiffFilter : IFilter
    {
        private const int KeyFrameAfterFrames = 20;
        private OutputPixel[] lastFrame;
        private int frameCount;

        public Task<OutputFrame> ApplyFilter(OutputFrame frame)
        {
            var pixels = OptimizeBandwidth(frame.Pixels, ref this.lastFrame);
            this.frameCount++;
            return Task.FromResult(new OutputFrame(frame.OffsetX, frame.OffsetY, pixels, frame.CacheId, frame.OffsetStatic));
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
                {
                    //newPixel = new OutputPixel(newPixel.X, newPixel.Y, Color.Magenta.ToArgb());
                    continue;
                }
                lastFrame[i] = newPixel;
                toReturn.Add(newPixel);
            }

            return toReturn.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ColorsEqual(OutputPixel oldPixel, OutputPixel newPixel)
        {
            const int threshold = 10;

            var c1 = oldPixel.Color;
            var c2 = newPixel.Color;

            if (c1 == c2)
                return true;

            var diff = (long)(c1 & 0xFF) - (c2 & 0xFF);
            if (diff > threshold || diff < -threshold)
                return false;
            c1 >>= 8;
            c2 >>= 8;

            diff = (long)(c1 & 0xFF) - (c2 & 0xFF);
            if (diff > threshold || diff < -threshold)
                return false;
            c1 >>= 8;
            c2 >>= 8;

            diff = (long)(c1 & 0xFF) - (c2 & 0xFF);
            if (diff > threshold || diff < -threshold)
                return false;

            return true;
        }

        public void Dispose()
        {
            this.lastFrame = null;
        }
    }
}
