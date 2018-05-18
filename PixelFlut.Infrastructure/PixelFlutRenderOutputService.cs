using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PixelFlut.Infrastructure
{
    public class PixelFlutRenderOutputService : IRenderOutputService<byte[]>
    {
        private Socket client;
        private readonly EndPoint endPoint;

        public PixelFlutRenderOutputService(EndPoint endPoint)
        {
            this.client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.endPoint = endPoint;
        }

        public void Output(byte[] rendered)
        {
            if (!(this.client?.Connected ?? false))
            {
                this.client?.Dispose();
                this.client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.client.Connect(this.endPoint);
            }

            try
            {
                //For debugging: set each pixel seperately
                // foreach (var px in Encoding.UTF8.GetString(rendered).Split('\n').Select(x => Encoding.UTF8.GetBytes(x+"\n")))
                // {
                //     Console.WriteLine(Encoding.UTF8.GetString(px));
                //     this.client.Send(px);
                //     System.Threading.Thread.Sleep(100);
                // }

                //For debugging: output Pixels to console
                //Console.WriteLine(Encoding.UTF8.GetString(rendered));

                //var sentBytes = this.client.Send(rendered);

                //for pure traffic generation measurement:
                var sentBytes = rendered.Length;

                lock (_diagSamples)
                {
                    _diagSamples.Enqueue(new Tuple<DateTime, int>(DateTime.UtcNow, sentBytes));

                    if (_diagSamples.Count > _numSamples)
                    {
                        _diagSamples.Dequeue();
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Connection fucked: {ex.SocketErrorCode}\n{ex}");
                if (ex.SocketErrorCode == SocketError.Shutdown ||
                    ex.SocketErrorCode == SocketError.ConnectionAborted ||
                    ex.SocketErrorCode == SocketError.TimedOut)
                {
                    this.client.Dispose();
                    this.client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    this.client.Connect(this.endPoint);
                }
            }
        }
        private const int _numSamples = 1000;
        private Queue<Tuple<DateTime, int>> _diagSamples = new Queue<Tuple<DateTime, int>>();

        public IList<KeyValuePair<string, string>> GetDiagnostics()
        {
            var toReturn = new List<KeyValuePair<string, string>>();

            IList<Tuple<DateTime, int>> diagSamples;
            lock (_diagSamples)
            {
                diagSamples = _diagSamples.ToList();
            }
            if (diagSamples.Count > 2)
            {
                var span = diagSamples.Max(x => x.Item1) - diagSamples.Min(x => x.Item1);
                var totalBytes = diagSamples.Sum(x => (long)x.Item2);

                var mbit = totalBytes / 1000d / 1000d * 8d;
                var bandwidthMBits = mbit / span.TotalSeconds;
                toReturn.Add(new KeyValuePair<string, string>("Bandwidth (Mbit/s)", bandwidthMBits.ToString("F2", CultureInfo.InvariantCulture)));
            }

            return toReturn;
        }

        private readonly byte[] px = Encoding.ASCII.GetBytes("PX ");
        private readonly byte newline = Encoding.ASCII.GetBytes("\n")[0];
        private readonly byte space = Encoding.ASCII.GetBytes(" ")[0];
        private readonly byte[][] numbers = Enumerable.Range(0, 5000).Select(x => Encoding.ASCII.GetBytes(x.ToString(CultureInfo.InvariantCulture))).ToArray();
        private readonly byte[][] hexNumbers = Enumerable.Range(0, 256).Select(x => Encoding.ASCII.GetBytes(x.ToString("X2"))).ToArray();

        public byte[] PreRender(IReadOnlyCollection<OutputPixel> pixels)
        {
            using (var ms = new MemoryStream(pixels.Count * 20))
            {
                foreach (var pixel in pixels)
                {
                    ms.Write(px, 0, 3);
                    var xNum = numbers[pixel.Point.X];
                    ms.Write(xNum, 0, xNum.Length);

                    ms.WriteByte(space);

                    var yNum = numbers[pixel.Point.Y];
                    ms.Write(yNum, 0, yNum.Length);

                    ms.WriteByte(space);

                    var argbColor = pixel.Color.ToArgb();

                    ms.Write(hexNumbers[argbColor >> 16 & 0xFF], 0, 2);
                    ms.Write(hexNumbers[argbColor >> 8 & 0xFF], 0, 2);
                    ms.Write(hexNumbers[argbColor & 0xFF], 0, 2);

                    var a = (argbColor >> 24 & 0xFF);
                    if (a != 255)
                    {
                        ms.Write(hexNumbers[a], 0, 2);
                    }
                    ms.WriteByte(newline);
                }
                return ms.ToArray();
            }
        }

        public Size GetSize()
        {
            while (true)
            {
                var bytes = new byte[1000];
                int receivedBytes;
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    Console.WriteLine("getting size");
                    socket.Connect(this.endPoint);
                    socket.Send(Encoding.UTF8.GetBytes("SIZE\n"));

                    receivedBytes = socket.Receive(bytes);

                    socket.Close();
                }
                var str = Encoding.UTF8.GetString(bytes, 0, receivedBytes);
                Console.WriteLine($"Got response: {str}");
                var split = str.Split(' ');
                try
                {
                    return new Size(int.Parse(split[1]), int.Parse(split[2]));
                }
                catch (IndexOutOfRangeException)
                {
                    continue;
                }
            }
        }
    }
}