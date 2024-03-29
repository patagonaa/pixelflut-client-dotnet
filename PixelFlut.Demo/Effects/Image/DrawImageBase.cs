using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PixelFlut.Infrastructure;

namespace PixelFlut.Demo.Effects.Image
{
    public abstract class DrawImageBase : EffectBase
    {
        protected const int BytesPerPixel = 4;
        protected const PixelFormat PixelFormatToUse = PixelFormat.Format32bppArgb;

        protected Bitmap GetImageData(string filePath)
        {
            Bitmap bitmap = new Bitmap(filePath);
            if(bitmap.PixelFormat != PixelFormatToUse)
            {
                Bitmap newBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormatToUse);

                using (Graphics gr = Graphics.FromImage(newBitmap))
                {
                    gr.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                }

                bitmap.Dispose();
                bitmap = newBitmap;
            }

            return bitmap;
        }

        protected OutputPixel[] DrawImage(Bitmap image, Point offset, bool mirror = false)
        {
            var offsetX = offset.X;
            var offsetY = offset.Y;

            var canvasSize = this.CanvasSize;

            if (image.PixelFormat != PixelFormatToUse)
            {
                throw new ArgumentException($"image must be {PixelFormatToUse}");
            }

            var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormatToUse);
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
                    int pixelIndex = ((mirror ? width - x - 1 : x) * BytesPerPixel + (y * stride));
                    var renderX = x + offsetX;
                    var renderY = y + offsetY;
                    if (renderX < 0 || renderY < 0 || renderX >= canvasWidth || renderY >= canvasHeight)
                        continue;

                    byte a = bytes[pixelIndex + 3];
                    byte r = bytes[pixelIndex + 2];
                    byte g = bytes[pixelIndex + 1];
                    byte b = bytes[pixelIndex];

                    uint argb = unchecked((uint)(a << 24 | r << 16 | g << 8 | b));
                    toReturn[i].X = (x + offsetX);
                    toReturn[i].Y = (y + offsetY);
                    toReturn[i].Color = argb;
                    i++;
                }
            }

            return toReturn;
        }
    }
}