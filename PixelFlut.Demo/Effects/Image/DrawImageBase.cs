using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PixelFlut.Infrastructure;

namespace PixelFlut.Demo.Effects.Image
{
    public abstract class DrawImageBase : EffectBase
    {

        protected Bitmap GetImageData(string filePath)
        {
            Bitmap bitmap = new Bitmap(filePath);
            if(bitmap.PixelFormat != PixelFormat.Format24bppRgb)
            {
                Bitmap newBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);

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

                    byte r = bytes[pixelIndex + 2];
                    byte g = bytes[pixelIndex + 1];
                    byte b = bytes[pixelIndex];

                    uint argb = unchecked((uint)(0xFF << 24 | r << 16 | g << 8 | b));
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