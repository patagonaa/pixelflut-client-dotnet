using System;
using System.Net;
using System.Threading;
using PixelFlut.Infrastructure;
using PixelFlut.Demo.Effects.Image;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;

namespace PixelFlut.Demo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var r = new Random();
            var ip = IPAddress.Parse("172.31.248.128");
            var port = 1234;

            var ep = new IPEndPoint(ip, port);

            //var renderService = new PixelFlutLookupTableRenderService();
            var renderService = new PixelFlutLookupTableUnsafeRenderService(ServerCapabilities.None);
            var outputService = new PixelFlutNullOutputService(new Size(1920, 1080));
            //var outputService = new PixelFlutTcpOutputService(ep);

            var eh = new EffectHost(renderService, outputService);
            //eh.AddEffect(new RandomBoxes(new Size(50, 50)));
            //eh.AddEffect(new RandomBoxes(new Size(500, 500)));
            //eh.SetEffect(new DrawImageStatic("/home/patagona/Stuff/cyber.jpg", Point.Empty));
            eh.AddEffect(new DrawImageSolitaire(Directory.GetFiles("Resources/cards"), 32));
            //eh.SetEffect(new DrawImageSolitaire(new List<string>{"/home/patagona/Stuff/solitaire.png"}, 50));
            //eh.SetEffect(new DrawImageSolitaire(new List<string>{"/home/patagona/Stuff/white.png", "/home/patagona/Stuff/black.png"}, 2));
            //eh.SetEffect(new Infrastructure.Effects.Void());
            eh.Start();

            var cts = new CancellationTokenSource();
            System.AppDomain.CurrentDomain.ProcessExit += (e, evArgs) => cts.Cancel();
            cts.Token.WaitHandle.WaitOne();

            await eh.Stop();
            renderService.Dispose();
        }
    }
}
