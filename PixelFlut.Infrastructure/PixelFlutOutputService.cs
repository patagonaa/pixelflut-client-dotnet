using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PixelFlut.Infrastructure
{
    public class PixelFlutTcpOutputService : IOutputService
    {
        private Socket client;
        private readonly EndPoint endPoint;

        public PixelFlutTcpOutputService(EndPoint endPoint)
        {
            this.client = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            this.client.DualMode = true;
            this.endPoint = endPoint;
        }

        public int Output(ArraySegment<byte> rendered)
        {
            try
            {
                EnsureConnected();

                //For debugging: set each pixel seperately
                // foreach (var px in Encoding.UTF8.GetString(rendered.Array, 0, rendered.Count).Split('\n').Select(x => Encoding.UTF8.GetBytes(x+"\n")))
                // {
                //     Console.WriteLine(Encoding.UTF8.GetString(px));
                //     this.client.Send(px);
                //     System.Threading.Thread.Sleep(100);
                // }

                //For debugging: output Pixels to console
                //Console.WriteLine(Encoding.UTF8.GetString(rendered));

                return this.client.Send(new[] { rendered });
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Connection fucked: {ex.SocketErrorCode}");
                return 0;
            }
        }

        private void EnsureConnected()
        {
            if (!(this.client?.Connected ?? false))
            {
                Thread.Sleep(1000);
                this.client?.Dispose();
                this.client = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                this.client.DualMode = true;
                this.client.Connect(this.endPoint);
            }
        }

        public Size GetSize()
        {
            while (true)
            {
                var bytes = new byte[1000];
                int receivedBytes;
                using (var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.DualMode = true;
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