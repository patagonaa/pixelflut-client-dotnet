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
using PixelFlut.Infrastructure.Effects.Image;

namespace PixelFlut.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var r = new Random();
            var ip = IPAddress.Parse("192.168.178.59");
            var port = 1234;

            var ep = new IPEndPoint(ip, port);

            var outputService = new PixelFlutRenderOutputService(ep);

            var eh = new EffectHost<byte[]>(outputService);
            eh.SetEffect(new DrawImageSolitaire("/home/patagona/Stuff/solitaire.png"));
            eh.Start();
            Thread.Sleep(Timeout.Infinite);
            eh.Stop();
        }
    }
}
