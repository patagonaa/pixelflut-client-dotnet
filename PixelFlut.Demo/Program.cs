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
using PixelFlut.Infrastructure.Effects;

namespace PixelFlut.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var r = new Random();
            var ip = IPAddress.Parse("127.0.0.1");
            var port = 1337;

            var ep = new IPEndPoint(ip, port);

            var outputService = new PixelFlutRenderOutputService(ep);

            var eh = new EffectHost<byte[]>(outputService);
            eh.SetEffect(new RandomBoxes(new Size(400, 400)));
            eh.Start();
            Thread.Sleep(600000);
            eh.Stop();
        }
    }
}
