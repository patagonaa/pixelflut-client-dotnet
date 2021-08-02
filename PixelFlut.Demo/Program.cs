using System;
using System.Net;
using System.Threading;
using PixelFlut.Infrastructure;
using PixelFlut.Demo.Effects.Image;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using PixelFlut.Demo.Effects;
using PixelFlut.Demo.Filters;

namespace PixelFlut.Demo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var ep = new DnsEndPoint("pixelflut.uwu.industries", 1234);
            //var ep = new IPEndPoint(IPAddress.Parse("193.30.122.10"), 1234);

            var renderService = new PixelFlutLookupTableUnsafeRenderService(ServerCapabilities.None);

            //var outputService = new PixelFlutNullOutputService(new Size(1920, 1080));
            var outputService = new PixelFlutTcpOutputService(ep);
            //var outputService = new FileOutputService("C:\\Temp\\cards.px");

            var eh = new EffectHost(outputService.GetSize(), renderService);

            //await eh.AddEffect(new RandomBoxes(new Size(500, 500)));
            //await eh.AddEffect(new DrawImageStatic(@"C:\Temp\Test.jpg", Point.Empty));
            await eh.AddEffect(new DrawImageSolitaire(Directory.GetFiles("Resources/cards"), 4));
            //await eh.AddEffect(new VideoPlayback(@"C:\Temp\Test.mp4"));
            //await eh.AddEffect(new Effects.Void());

            await eh.AddOutput(outputService);
            eh.Start();

            var cts = new CancellationTokenSource();
            AppDomain.CurrentDomain.ProcessExit += (e, evArgs) => cts.Cancel();
            cts.Token.WaitHandle.WaitOne();

            eh.Stop();
            renderService.Dispose();
        }
    }
}
