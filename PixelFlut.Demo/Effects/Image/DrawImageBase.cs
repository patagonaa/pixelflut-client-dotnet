using System;
using System.Drawing;
using Rgba32Image = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.Rgba32>;
using ImageExtensions = SixLabors.ImageSharp.ImageExtensions;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PixelFlut.Infrastructure;

namespace PixelFlut.Demo.Effects.Image
{
    public abstract class DrawImageBase : EffectBase
    {

        protected Tuple<Rgba32Image, byte[]> GetImageData(string filePath)
        {
            var image = SixLabors.ImageSharp.Image.Load(filePath);
            var pixelData = ImageExtensions.SavePixelData(image);
            return new Tuple<Rgba32Image, byte[]>(image, pixelData);
        }

        protected OutputPixel[] DrawImage(Bitmap image, Point offset, bool mirror = false)
        {
            var offsetX = offset.X;
            var offsetY = offset.Y;

            var canvasSize = this.CanvasSize;

            if (image.PixelFormat != PixelFormat.Format24bppRgb)
            {
                throw new ArgumentException("image must be 24 bpp RGB");
            }

            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var stride = bitmapData.Stride;
            var dataLength = stride * bitmapData.Height;
            var bytes = new byte[dataLength];
            Marshal.Copy(bitmapData.Scan0, bytes, 0, dataLength);
            image.UnlockBits(bitmapData);

            var width = image.Width;
            var height = image.Height;

            var canvasWidth = canvasSize.Width;
            var canvasHeight = canvasSize.Height;

            var toReturn = new OutputPixel[width * height];

            int i = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = ((mirror ? width - x - 1 : x) * 3 + (y * stride));
                    var renderX = x + offsetX;
                    var renderY = y + offsetY;
                    if (renderX < 0 || renderY < 0 || renderX >= canvasWidth || renderY >= canvasHeight)
                        continue;

                    int r = bytes[pixelIndex + 2];
                    int g = bytes[pixelIndex + 1];
                    int b = bytes[pixelIndex];

                    var argb = 0xFF << 24 | r << 16 | g << 8 | b;
                    toReturn[i].X = (x + offsetX);
                    toReturn[i].Y = (y + offsetY);
                    toReturn[i].Color = argb;
                    i++;
                }
            }

            return toReturn;
        }

        protected IEnumerable<OutputPixel> DrawImage(Tuple<Rgba32Image, byte[]> imageData, Point offset, bool mirror = false)
        {
            var offsetX = offset.X;
            var offsetY = offset.Y;

            var canvasSize = this.CanvasSize;
            var image = imageData.Item1;
            var bytes = imageData.Item2;

            var width = image.Width;
            var height = image.Height;

            var canvasWidth = canvasSize.Width;
            var canvasHeight = canvasSize.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = ((mirror ? width - x - 1 : x) + (y * width)) * 4;
                    int a = bytes[pixelIndex + 3];
                    if (a == 0)
                        continue;
                    var renderX = x + offsetX;
                    var renderY = y + offsetY;
                    if (renderX < 0 || renderY < 0 || renderX >= canvasWidth || renderY >= canvasHeight)
                        continue;

                    int r = bytes[pixelIndex];
                    int g = bytes[pixelIndex + 1];
                    int b = bytes[pixelIndex + 2];

                    var argb = a << 24 | r << 16 | g << 8 | b;
                    yield return new OutputPixel(x + offsetX, y + offsetY, argb);
                }
            }
        }
    }
}