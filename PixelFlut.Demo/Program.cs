using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using PixelFlut.Infrastructure;

namespace PixelFlut.Demo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var r = new Random();
            var ip = IPAddress.Parse("192.168.178.148");
            using (var client = new Client())
            {
                await client.Connect(ip, 3141);
                var tasks = new List<Task>();
                var c = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        client.SetPixel(x, y, (x / 4 + y / 4) % 2 == 1 ? c : Color.Black);
                    }
                    Console.WriteLine($"Line {y} written");
                }
            }
            Console.WriteLine("Ended normally");
        }
    }
}
