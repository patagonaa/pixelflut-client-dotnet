using System;
using System.Collections.Generic;
using System.Drawing;

namespace PixelFlut.Infrastructure.Effects
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

        protected override IEnumerable<OutputPixel> TickInternal()
        {
            var maxOffsetX = CanvasSize.Width - boxSize.Width + 1;
            var maxOffsetY = CanvasSize.Height - boxSize.Height + 1;

            var color = ((int)(random.Next() | 0xFF000000));
            //var color = random.Next(0, 100) > 50 ? Color.White : Color.Black;
            var offsetX = this.random.Next(0, maxOffsetX);
            var offsetY = this.random.Next(0, maxOffsetY);

            var toReturn = new OutputPixel[boxSize.Width * boxSize.Height];

            int i = 0;
            for (int y = 0; y < boxSize.Height; y++)
            {
                for (int x = 0; x < boxSize.Width; x++)
                {
                    toReturn[i++] = new OutputPixel(offsetX + x, offsetY + y, color);
                }
            }

            return toReturn;
        }
    }
}