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
        private OutputPixel[] renderedImage;

        public DrawImageStatic(string filePath, Point p)
        {
            this.image = GetImageData(filePath);
            this.pos = p;
            this.random = new Random();
        }

        protected override OutputFrame TickInternal()
        {
            OutputPixel[] pixels = renderedImage ?? (renderedImage = DrawImage(this.image, Point.Empty).OrderBy(x => random.Next()).ToList().ToArray());
            return new OutputFrame(pos.X, pos.Y, pixels);
        }
    }
}