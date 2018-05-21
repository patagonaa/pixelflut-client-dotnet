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
                    int a = bytes[offset + 3];
                    if (a == 0)
                        continue;
                    var renderX = x + offsetX;
                    var renderY = y + offsetY;
                    if (renderX < 0 || renderY < 0 || renderX >= canvasSize.Width || renderY >= canvasSize.Height)
                        continue;

                    int r = bytes[offset];
                    int g = bytes[offset + 1];
                    int b = bytes[offset + 2];

                    var argb = a << 24 | r << 16 | g << 8 | b;
                    yield return new OutputPixel(x + offsetX, y + offsetY, argb);
                }
            }
        }
    }
}