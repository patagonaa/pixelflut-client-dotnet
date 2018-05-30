using PixelFlut.Infrastructure;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace PixelFlut.Demo.Effects
{
    public class Void : EffectBase
    {
        protected override OutputFrame TickInternal()
        {
            var height = CanvasSize.Height;
            var width = CanvasSize.Width;
            var color = Color.White.ToArgb();

            var toReturn = new OutputPixel[width * height];
            var i = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    toReturn[i++] = new OutputPixel(x, y, color);
                }
            }

            return new OutputFrame(0, 0, toReturn, 0, true);
        }
    }
}