using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PixelFlut.Infrastructure
{
    public class PixelFlutRenderOutputService : IRenderOutputService<byte[]>
    {
        private Socket client;
        private readonly IPEndPoint endPoint;

        public PixelFlutRenderOutputService(IPEndPoint endPoint)
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
                this.client.Send(rendered);
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

        public byte[] PreRender(IReadOnlyCollection<OutputPixel> pixels)
        {
            using (var ms = new MemoryStream(pixels.Count * 20))
            {
                using (var sw = new StreamWriter(ms))
                {
                    sw.NewLine = "\n";
                    foreach (var pixel in pixels)
                    {
                        sw.Write("PX ");
                        sw.Write(pixel.Point.X);
                        sw.Write(" ");
                        sw.Write(pixel.Point.Y);
                        sw.Write(" ");
                        var argbColor = pixel.Color.ToArgb();

                        //R
                        sw.Write(GetHexValue(argbColor >> 20 & 0xF));
                        sw.Write(GetHexValue(argbColor >> 16 & 0xF));

                        //G
                        sw.Write(GetHexValue(argbColor >> 12 & 0xF));
                        sw.Write(GetHexValue(argbColor >> 8 & 0xF));

                        //B
                        sw.Write(GetHexValue(argbColor >> 4 & 0xF));
                        sw.Write(GetHexValue(argbColor & 0xF));

                        //A
                        if ((argbColor >> 24 & 0xFF) != 255)
                        {
                            sw.Write(GetHexValue(argbColor >> 28 & 0xF));
                            sw.Write(GetHexValue(argbColor >> 24 & 0xF));
                        }

                        sw.WriteLine();
                    }
                    sw.Flush();
                    return ms.ToArray();
                }
            }
        }

        private static char GetHexValue(int i)
        {
            Contract.Assert(i >= 0 && i < 16, "i is out of range.");
            if (i < 10)
            {
                return (char)(i + '0');
            }

            return (char)(i - 10 + 'A');
        }

        public Size GetSize()
        {
            while (true)
            {
                var bytes = new byte[1000];
                int receivedBytes;
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(this.endPoint);
                    socket.Send(Encoding.UTF8.GetBytes("SIZE\n"));

                    receivedBytes = socket.Receive(bytes);

                    socket.Close();
                }
                var str = Encoding.UTF8.GetString(bytes, 0, receivedBytes);
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