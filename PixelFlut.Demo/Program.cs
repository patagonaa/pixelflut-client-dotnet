using System;
using System.Collections.Generic;
using System.Drawing;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using PixelFlut.Infrastructure;
using Image = SixLabors.ImageSharp.Image;
using Rgba32Image = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.Rgba32>;
using ImageExtensions = SixLabors.ImageSharp.ImageExtensions;
using System.Net.Sockets;
using System.Diagnostics;

namespace PixelFlut.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var r = new Random();
            var ip = IPAddress.Parse("192.168.178.74");
            var port = 1337;
            using (var client = new Client(ip, port))
            {
                Rgba32Image img = Image.Load("/home/patagona/Stuff/solitaire.png");
                var pixelData = ImageExtensions.SavePixelData(img);

                var canvasSize = client.GetSize();
                client.Connect();

                var speedX = 5d;
                var speedY = 0d;

                var offsetX = 250d;
                var offsetY = 0d;

                while (true)
                {
                    offsetX += speedX;
                    offsetY += speedY;

                    if (offsetY + img.Height > canvasSize.Height)
                    {
                        speedY = -(speedY / 1.1);
                    }
                    else
                    {
                        speedY += 1;
                    }

                    if (offsetX + img.Width > canvasSize.Width)
                    {
                        speedX = -speedX;
                    }

                    if (offsetX < 250)
                    {
                        speedX = -speedX;
                    }

                    if (Math.Abs(speedY) < 0.1 && (canvasSize.Height - (offsetY + img.Height) < 10))
                    {
                        speedY = 0d;

                        offsetX = r.Next(250, canvasSize.Width - img.Width);
                        offsetY = 0d;
                    }

                    DrawImage(client, img, pixelData, canvasSize, new Point((int)offsetX, (int)offsetY));

                    client.Write();
                    client.Clear();
                    //Thread.Sleep(100);

                    //Console.WriteLine("Image Drawn");
                }
            }
        }
        private static void ShowTime(Size canvasSize, Random r, Client client)
        {
            var c = Color.FromArgb(r.Next(255), r.Next(255), r.Next(255));
            var img = new Bitmap(canvasSize.Width, canvasSize.Height);
            SizeF textSize;
            using (var g = Graphics.FromImage(img))
            {
                var str = $"{DateTime.Now:HH:mm:ss}";
                var font = new Font(FontFamily.GenericMonospace, 120);
                textSize = g.MeasureString(str, font);
                g.DrawString(str, font, Brushes.White, 0, 0);
            }
            for (int y = 0; y < textSize.Height; y++)
            {
                for (int x = 0; x < textSize.Width; x++)
                {
                    var color = img.GetPixel(x, y);
                    if (color.A == 0)
                        color = Color.Black;
                    client.SetPixel(x, y, color);
                }
            }
        }

        private static void Rotate2D(ref double point1, ref double point2, double axis1, double axis2, double ang)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (ang == 0)
                return;
            // ReSharper restore CompareOfFloatsByEqualityOperator

            var posX = point1 - axis1;
            var posY = point2 - axis2;

            var r = Math.Sqrt(Math.Pow(posX, 2) + Math.Pow(posY, 2));

            var theta = Math.Atan2(posY, posX) + ang;
            point1 = r * Math.Cos(theta) + axis1;
            point2 = r * Math.Sin(theta) + axis2;
        }

        static IList<Point> DrawLine(Client client, int x1, int y1, int x2, int y2)
        {
            var points = new List<Point>();

            if (x2 < x1)
            {
                var tmp = x2;
                x2 = x1;
                x1 = tmp;
                tmp = y1;
                y1 = y2;
                y2 = tmp;
            }
            else if (x1 == x2)
            {
                for (var y = y1; y != y2; y += Math.Sign(y2 - y1)) // draws one pixel too little
                {
                    points.Add(new Point(x1, y));
                }
                return points;
            }

            var dx = x2 - x1;
            var dy = y2 - y1;

            for (var x = x1; x <= x2; x++)
            {
                int y = (int)(y1 + dy * (x - x1) / dx);
                points.Add(new Point(x, y));
            }

            return points;
        }

        static void DrawImage(Client client, Rgba32Image image, byte[] bytes, Size canvasSize, Point offsetP)
        {
            var r = new Random();
            // var offsetX = r.Next(-image.Width, canvasSize.Width);
            // var offsetY = r.Next(-image.Height, canvasSize.Height);
            // var mirror = r.Next(0, 100) > 50;

            var offsetX = offsetP.X;
            var offsetY = offsetP.Y;
            var mirror = false;

            var pixels = new List<Point>();

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    pixels.Add(new Point(x, y));
                }
            }

            int skipPixel = 10;

            var sw = Stopwatch.StartNew();
            foreach (var point in pixels.OrderBy(x => (x.X + (x.Y * skipPixel)) % skipPixel == 0).ThenBy(x => x.Y))
            {
                var x = point.X;
                var y = point.Y;

                int offset = ((mirror ? image.Width - x - 1 : x) + (y * image.Width)) * 4;
                if (bytes[offset + 3] == 0)
                    continue;
                var renderX = x + offsetX;
                var renderY = y + offsetY;
                if (renderX < 0 || renderY < 0 || renderX >= canvasSize.Width || renderY >= canvasSize.Height)
                    continue;
                client.SetPixel(x + offsetX, y + offsetY, Color.FromArgb(bytes[offset + 3], bytes[offset], bytes[offset + 1], bytes[offset + 2]));
            }
            sw.Stop();
            Console.WriteLine($"pps: {pixels.Count * 1000 / sw.ElapsedMilliseconds}");
        }
    }
}
