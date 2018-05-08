using System;
using System.Drawing;
using Rgba32Image = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.Rgba32>;
using ImageExtensions = SixLabors.ImageSharp.ImageExtensions;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using System.Linq;

namespace PixelFlut.Infrastructure.Effects.Image
{
    public abstract class DrawImageBase : EffectBase
    {
        protected readonly Rgba32Image image;
        protected readonly byte[] pixelData;

        public DrawImageBase(string filePath)
        {
            this.image = SixLabors.ImageSharp.Image.Load(filePath);
            this.pixelData = ImageExtensions.SavePixelData(this.image);
        }

        protected IEnumerable<OutputPixel> DrawImage(Point offsetP, bool mirror = false)
        {
            var offsetX = offsetP.X;
            var offsetY = offsetP.Y;

            var canvasSize = this.CanvasSize;
            var bytes = this.pixelData;

            var width = image.Width;
            var height = image.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = ((mirror ? width - x - 1 : x) + (y * width)) * 4;
                    if (bytes[offset + 3] == 0)
                        continue;
                    var renderX = x + offsetX;
                    var renderY = y + offsetY;
                    if (renderX < 0 || renderY < 0 || renderX >= canvasSize.Width || renderY >= canvasSize.Height)
                        continue;

                    var imagePos = new Point(x, y);
                    imagePos.Offset(offsetX, offsetY);

                    yield return new OutputPixel(imagePos, Color.FromArgb(bytes[offset + 3], bytes[offset], bytes[offset + 1], bytes[offset + 2]));
                }
            }
        }
    }
}