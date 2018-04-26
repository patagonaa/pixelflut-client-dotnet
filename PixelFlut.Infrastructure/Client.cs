using System;
using System.Collections.Generic;
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
        private Socket _socket;
        private readonly IPAddress _ip;
        private readonly int _port;

        public Client(IPAddress ip, int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.SendBufferSize = 50 * 1024 * 1024;
            _socket.SendTimeout = 1000;
            _ip = ip;
            _port = port;
        }

        public void Connect()
        {
            _socket.Dispose();
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.SendBufferSize = 50 * 1024 * 1024;
            _socket.SendTimeout = 1000;
            _socket.Connect(_ip, _port);
        }

        public void Dispose()
        {
            _socket.Close();
            _socket.Dispose();
        }

        private object _msgLock = new object();
        private StringBuilder _msg = new StringBuilder(100000000);

        public void SetPixel(int x, int y, Color color)
        {
            lock (_msgLock)
            {
                _msg.Append("PX ");
                _msg.Append(x);
                _msg.Append(" ");
                _msg.Append(y);
                _msg.Append(" ");
                _msg.Append(color.R.ToString("X2"));
                _msg.Append(color.G.ToString("X2"));
                _msg.Append(color.B.ToString("X2"));
                if (color.A != 255)
                    _msg.Append(color.A.ToString("X2"));
                _msg.Append("\n");
            }
        }

        public void Write()
        {
            try
            {
                string msg;

                //lock (_msgLock)
                {
                    msg = _msg.ToString();
                }

                _socket.Send(Encoding.UTF8.GetBytes(msg));

                //Console.Write(msg);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Connection fucked: {ex.SocketErrorCode}\n{ex}");
                if (ex.SocketErrorCode == SocketError.Shutdown ||
                    ex.SocketErrorCode == SocketError.ConnectionAborted ||
                    ex.SocketErrorCode == SocketError.TimedOut)
                {
                    Connect();
                }
            }
        }

        public void Clear()
        {
            lock (_msgLock)
            {
                _msg.Clear();
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
                    socket.Connect(_ip, _port);
                    socket.Send(Encoding.UTF8.GetBytes("SIZE\n"));

                    receivedBytes = socket.Receive(bytes);

                    socket.Close();
                }
                var str = Encoding.UTF8.GetString(bytes, 0, receivedBytes);
                //Console.WriteLine($"SIZE answer: {str}");
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
