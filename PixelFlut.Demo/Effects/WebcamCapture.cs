// using OpenCvSharp;
// using OpenCvSharp.Extensions;
// using System.Drawing;
// using Size = System.Drawing.Size;
// using Point = System.Drawing.Point;
// using System;
// using System.Drawing.Imaging;
// using System.Runtime.InteropServices;

// namespace PixelFlut.Infrastructure.Effects
// {
//     public class WebcamCapture : EffectBase
//     {
//         private VideoCapture capture;
//         private Mat frame;

//         public WebcamCapture()
//         {
//             this.capture = new VideoCapture();

//             this.frame = new Mat();
//             capture.Open(0);

//             if (!capture.IsOpened())
//                 throw new Exception("capture initialization failed");
//         }

//         protected override void TickInternal()
//         {

//             capture.Read(frame);

//             if (frame.Channels() != 3)
//                 throw new ArgumentException("Frame should have RGB");

//             var bitmap = new Bitmap(frame.Width, frame.Height, PixelFormat.Format24bppRgb);

//             BitmapConverter.ToBitmap(frame, bitmap);

//             var offsetX = 0;
//             var offsetY = 0;

//             var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, bitmap.PixelFormat);
//             var stride = data.Stride;
//             var buffer = new byte[stride * data.Height];
//             Marshal.Copy(data.Scan0, buffer, 0, stride * data.Height);
//             bitmap.UnlockBits(data);

//             var width = bitmap.Width;
//             var height = bitmap.Height;

//             for (int y = 0; y < height; y++)
//             {
//                 for (int x = 0; x < width; x++)
//                 {
//                     var pos = new Point(offsetX + x, offsetY + y);

//                     var offset = ((x * 3) + (y * stride));
//                     var r = buffer[offset + 2];
//                     var g = buffer[offset + 1];
//                     var b = buffer[offset];

//                     DrawPixel(pos, Color.FromArgb(255, r, g, b));
//                 }
//             }
//         }
//     }
// }