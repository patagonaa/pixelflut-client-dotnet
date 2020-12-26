using PixelFlut.Infrastructure;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace PixelFlut.Demo.Effects
{
    public class RandomBoxes : EffectBase
    {
        private readonly Size boxSize;
        private readonly Random random;

        public RandomBoxes(Size boxSize)
        {
            this.boxSize = boxSize;
            this.random = new Random();
        }

        protected override OutputFrame TickInternal()
        {
            var maxOffsetX = CanvasSize.Width - boxSize.Width + 1;
            var maxOffsetY = CanvasSize.Height - boxSize.Height + 1;

            var color = (uint)random.Next() | 0xFF000000;
            //var color = random.Next(0, 100) > 50 ? Color.White : Color.Black;
            var offsetX = this.random.Next(0, maxOffsetX);
            var offsetY = this.random.Next(0, maxOffsetY);

            var toReturn = new OutputPixel[boxSize.Width * boxSize.Height];

            int i = 0;
            var width = boxSize.Width;
            var height = boxSize.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    toReturn[i++] = new OutputPixel(x, y, color);
                }
            }

            return new OutputFrame(offsetX, offsetY, toReturn);
        }
    }
}