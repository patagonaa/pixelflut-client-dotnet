using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using PixelFlut.Infrastructure;
using SixLabors.ImageSharp;

namespace PixelFlut.Demo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var r = new Random();
            var ip = IPAddress.Parse("192.168.178.63");
            using (var client = new Client())
            {
                var image = Image.Load("/tmp/Downloads/kadse.png");
                var bytes = image.SavePixelData();

                await client.Connect(ip, 1234);
                var canvasSize = client.GetSize();
                var tasks = new List<Task>();
                while (true)
                {
                    var c = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                    var offsetX = r.Next(-image.Width, canvasSize.Width);
                    var offsetY = r.Next(-image.Height, canvasSize.Height);

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            int offset = (x + (y * image.Width)) * 4;
                            if(bytes[offset+3] == 0)
                                continue;
                            var renderX = x + offsetX;
                            var renderY = y + offsetY;
                            if(renderX < 0 || renderY < 0 || renderX >= canvasSize.Width || renderY >= canvasSize.Height)
                                continue;
                            client.SetPixel(x + offsetX, y + offsetY, Color.FromArgb(bytes[offset+3],bytes[offset],bytes[offset+1],bytes[offset+2]));
                        }
                        Console.WriteLine($"Line {y} written");
                    }
                }
            }
            Console.WriteLine("Ended normally");
        }
    }
}
