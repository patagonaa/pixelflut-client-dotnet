using System;
using System.Linq;
using System.Drawing;
using PixelFlut.Infrastructure;
using System.Threading.Tasks;

namespace PixelFlut.Demo.Effects.Image
{
    public class DrawImageStatic : DrawImageBase
    {
        private readonly Bitmap image;
        private readonly Point pos;
        private readonly Random random;
        private OutputPixel[] renderedImage;

        public DrawImageStatic(string filePath, Point p)
        {
            this.image = GetImageData(filePath);
            this.pos = p;
            this.random = new Random();
        }

        protected override Task<OutputFrame> TickInternal()
        {
            OutputPixel[] pixels = renderedImage ?? (renderedImage = DrawImage(this.image, Point.Empty).ToArray());
            return Task.FromResult(new OutputFrame(pos.X, pos.Y, pixels, 0, true));
        }
    }
}