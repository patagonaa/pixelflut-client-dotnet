using System;
using System.Collections.Generic;
using System.Drawing;

namespace PixelFlut.Infrastructure.Effects
{
    public class Void : EffectBase
    {
        protected override IEnumerable<OutputPixel> TickInternal()
        {
            var height = CanvasSize.Height;
            var width = CanvasSize.Width;
            var color = Color.Black.ToArgb();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    yield return new OutputPixel(x, y, color);
                }
            }
        }
    }
}