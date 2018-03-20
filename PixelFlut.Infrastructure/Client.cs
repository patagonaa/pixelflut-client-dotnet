using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PixelFlut.Infrastructure
{
    public class Client : IDisposable
    {
        private readonly Socket _socket;

        public Client()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
        }

        public async Task Connect(IPAddress ip, int port)
        {
            await _socket.ConnectAsync(ip, port);
        }

        public void Dispose()
        {
            _socket.Close();
            _socket.Dispose();
        }

        public void SetPixel(int x, int y, Color color)
        {
            var msg = $"PX {x} {y} {color.R:X2}{color.G:X2}{color.B:X2}";
            if (color.A != 255)
                msg += $"{color.A:X2}";
            msg += "\n";
            _socket.Send(Encoding.UTF8.GetBytes(msg));
        }
    }
}
