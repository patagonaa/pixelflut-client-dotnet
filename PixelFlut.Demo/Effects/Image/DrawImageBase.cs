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

        protected Tuple<Rgba32Image, byte[]> GetImageData(string filePath)
        {
            var image = SixLabors.ImageSharp.Image.Load(filePath);
            var pixelData = ImageExtensions.SavePixelData(image);
            return new Tuple<Rgba32Image, byte[]>(image, pixelData);
        }

        protected IEnumerable<OutputPixel> DrawImage(Tuple<Rgba32Image, byte[]> imageData, Point offsetP, bool mirror = false)
        {
            var offsetX = offsetP.X;
            var offsetY = offsetP.Y;

            var canvasSize = this.CanvasSize;
            var image = imageData.Item1;
            var bytes = imageData.Item2;

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