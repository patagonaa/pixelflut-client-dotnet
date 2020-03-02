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
using PixelFlut.Demo.Effects;
using PixelFlut.Demo.Effects.Image;

namespace PixelFlut.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var r = new Random();
            var ip = IPAddress.Parse("172.31.248.128");
            var port = 1234;

            var ep = new IPEndPoint(ip, port);
            //var ep = new DnsEndPoint("patagona-p51", port);

            //var renderService = new PixelFlutLookupTableRenderService();
            var renderService = new PixelFlutLookupTableUnsafeRenderService(ServerCapabilities.None);
            //var outputService = new PixelFlutOutputService(ep);

            var eh = new EffectHost(renderService, ep);
            eh.AddEffect(new RandomBoxes(new Size(500, 500)));
            eh.AddEffect(new RandomBoxes(new Size(500, 500)));
            //eh.SetEffect(new DrawImageStatic("/home/patagona/Stuff/cyber.jpg", Point.Empty));
            //eh.SetEffect(new DrawImageSolitaire(new List<string>{"/home/patagona/Stuff/cyber.jpg"}, 1));
            //eh.SetEffect(new DrawImageSolitaire(new List<string>{"/home/patagona/Stuff/solitaire.png"}, 50));
            //eh.SetEffect(new DrawImageSolitaire(new List<string>{"/home/patagona/Stuff/white.png", "/home/patagona/Stuff/black.png"}, 2));
            //eh.SetEffect(new Infrastructure.Effects.Void());
            eh.Start();

            var cts = new CancellationTokenSource();
            System.AppDomain.CurrentDomain.ProcessExit += (e, evArgs) => cts.Cancel();
            cts.Token.WaitHandle.WaitOne();

            eh.Stop();
            renderService.Dispose();
        }
    }
}
