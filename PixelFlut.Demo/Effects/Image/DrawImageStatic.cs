using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;

namespace PixelFlut.Infrastructure.Effects.Image
{
    public class DrawImageStatic : DrawImageBase
    {
        private readonly Point pos;
        private readonly Random random;

        public DrawImageStatic(string filePath, Point p) : base(filePath)
        {
            this.pos = p;
            this.random = new Random();
        }

        protected override IEnumerable<OutputPixel> TickInternal()
        {
            return DrawImage(this.pos).OrderBy(x => this.random.Next());
        }
    }
}