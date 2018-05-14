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

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pos = new Point(x, y);
                    yield return new OutputPixel(pos, Color.Black);
                }
            }
        }
    }
}