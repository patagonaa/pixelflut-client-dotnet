using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using SixLabors.ImageSharp;

namespace PixelFlut.Infrastructure.Effects.Image
{
    public class DrawImageStatic : DrawImageBase
    {
        private readonly Tuple<Image<Rgba32>, byte[]> image;
        private readonly Point pos;
        private readonly Random random;

        public DrawImageStatic(string filePath, Point p)
        {
            this.image = GetImageData(filePath);
            this.pos = p;
            this.random = new Random();
        }

        protected override IEnumerable<OutputPixel> TickInternal()
        {
            return DrawImage(this.image, this.pos).OrderBy(x => this.random.Next());
        }
    }
}