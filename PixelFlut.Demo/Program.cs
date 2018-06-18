using System;
using Size = System.Drawing.Size;
using System.Net;
using System.Threading;
using PixelFlut.Infrastructure;
using PixelFlut.Demo.Effects;
using PixelFlut.Demo.Filters;
using System.Threading.Tasks;

namespace PixelFlut.Demo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var r = new Random();
            var ip = IPAddress.Parse("10.214.11.105");
            var port = 1234;

            var ep = new IPEndPoint(ip, port);
            //var ep = new DnsEndPoint("displays.local", port);

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

            await eh.Stop();
            renderService.Dispose();
        }
    }
}
