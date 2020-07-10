using PixelFlut.Infrastructure;
using System.Collections.Generic;

namespace PixelFlut.Demo.Filters
{
    class InterlaceFilter : IFilter
    {
        private int frameCount;

        public OutputFrame ApplyFilter(OutputFrame frame)
        {
            var pixels = OptimizeBandwidth(frame.Pixels);
            this.frameCount++;
            return new OutputFrame(frame.OffsetX, frame.OffsetY, pixels, frame.CacheId, frame.OffsetStatic);
        }

        private OutputPixel[] OptimizeBandwidth(OutputPixel[] outputPixels)
        {
            var toReturn = new List<OutputPixel>(100000);

            var pixelLength = outputPixels.Length;
            for (int i = 0; i < pixelLength; i++)
            {
                var newPixel = outputPixels[i];

                if ((this.frameCount + i) % 2 == 0)
                    continue;
                toReturn.Add(newPixel);
            }

            return toReturn.ToArray();
        }

        public void Dispose()
        {
        }
    }
}
